// Поместите этот файл в папку Services (создайте её, если нет)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // Нужно для IOptions
using QuoteGeneratorWorker.Handlers;
using QuoteGeneratorWorker.Models;
using QuoteGeneratorWorker.Options; // Нужно для StockQuoteOptions

namespace QuoteGeneratorWorker.Services
{
    public class QuoteGeneratorService : BackgroundService
    {
        private readonly ILogger<QuoteGeneratorService> _logger;
        private readonly IQuoteHandler _handler; // Текущая реализация использует один хендлер
        private readonly string[] _symbols;
        private readonly int _intervalSeconds;
        private readonly Channel<StockQuote> _channel;
        private readonly Random _random = new();

        public QuoteGeneratorService(
            IQuoteHandler handler,
            ILogger<QuoteGeneratorService> logger,
            IOptions<StockQuoteOptions> options)
        {
            _handler = handler;
            _logger = logger;

            // Читаем настройки из appsettings.json
            _intervalSeconds = options.Value.IntervalSeconds;
            _symbols = options.Value.Symbols.ToArray();

            // Создаем канал (очередь) с ограниченным размером для защиты от переполнения памяти
            _channel = Channel.CreateBounded<StockQuote>(new BoundedChannelOptions(100) { SingleWriter = true });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");

            // Запускаем потребителя (обработчика) в отдельной задаче
            var consumerTask = ProcessQuotesFromQueueAsync(_channel.Reader, stoppingToken);

            // Запускаем производителя (поставщика)
            await GenerateQuotesToQueueAsync(_channel.Writer, stoppingToken);

            // Ждем завершения обработчика, корректно обрабатывая отмену
            try
            {
                await consumerTask;
                _logger.LogDebug("--- Потребитель успешно завершил обработку очереди. ---");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("--- Получен сигнал отмены. Потребитель завершил работу. ---");
            }

            _logger.LogInformation("🛑 Генератор котировок остановлен.");
        }

        #region Producer: Генерирует котировки и кладет их в очередь

        private async Task GenerateQuotesToQueueAsync(ChannelWriter<StockQuote> writer, CancellationToken stoppingToken)
        {
            try
            {
                DateTime nextRun = DateTime.UtcNow;

                while (!stoppingToken.IsCancellationRequested)
                {
                    var quotesBatch = GenerateQuotesBatch();
                    _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) добавлена в очередь. ---");

                    foreach (var quote in quotesBatch)
                    {
                        // Если очередь переполнена, ждем возможности записать или выходим при отмене
                        await writer.WaitToWriteAsync(stoppingToken);
                        writer.TryWrite(quote);
                    }

                    // Корректная задержка без выбрасывания исключения OperationCanceledException
                    nextRun = nextRun.AddSeconds(_intervalSeconds);
                    var delayTask = Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);

                    await Task.WhenAny(delayTask, Task.Delay(Timeout.Infinite, stoppingToken));
                    if (!delayTask.IsCompletedSuccessfully)
                    {
                        break; // Выходим из цикла, если задержка была отменена
                    }
                }
            }
            finally
            {
                // Сигнализируем потребителю, что новых котировок больше не будет
                writer.Complete();
                _logger.LogDebug("--- Производитель завершил работу. Очередь закрыта для записи. ---");
            }
        }
        #endregion

        #region Consumer: Читает котировки из очереди и обрабатывает их

        private async Task ProcessQuotesFromQueueAsync(ChannelReader<StockQuote> reader, CancellationToken stoppingToken)
        {
            await foreach (var quote in reader.ReadAllAsync(stoppingToken))
            {
                await _handler.HandleAsync(quote, stoppingToken);
            }
        }
        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Генерирует пачку котировок на основе списка символов из конфигурации.
        /// </summary>
        private List<StockQuote> GenerateQuotesBatch()
        {
            return _symbols.Select(symbol => new StockQuote(
                    symbol,
                    GenerateRandomPrice()
                )).ToList();
        }

        /// <summary>
        /// Генерирует случайную цену.
        /// </summary>
        private decimal GenerateRandomPrice()
        {
            var offset = _random.Next(-2000, 2001) / 100m; // Смещение от -20.00 до 20.00
            return Math.Round(150m + offset, 2);
        }

        #endregion
    }
}

//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using QuoteGeneratorWorker.Handlers;
//using QuoteGeneratorWorker.Models;
//using QuoteGeneratorWorker.Options;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace QuoteGeneratorWorker.Services;

//public class QuoteGeneratorService : BackgroundService
//{
//    private readonly ILogger<QuoteGeneratorService> _logger;
//    private readonly IQuoteHandler _handler; // Если вы хотите использовать один конкретный хендлер,
//                                             // но мы будем использовать несколько!

//    // --- ИЗМЕНЕНИЯ ---
//    private readonly string[] _symbols;
//    private readonly int _intervalSeconds;

//    // Если вы хотите использовать все хендлеры через IEnumerable<IQuoteHandler>, см. ниже.

//    public QuoteGeneratorService(
//        ILogger<QuoteGeneratorService> logger,
//        IOptions<StockQuoteOptions> options) // <-- Внедряем настройки здесь или через IEnumerable<IQuoteHandler>
//    {
//        _logger = logger;

//        // Читаем значения из конфигурации
//        _intervalSeconds = options.Value.IntervalSeconds;
//        _symbols = options.Value.Symbols.ToArray();

//        // Логика для работы с несколькими хендлерами (рекомендуемый подход)
//        // В этом случае конструктор будет выглядеть так:
//        // public QuoteGeneratorService(IEnumerable<IQuoteHandler> handlers, ...)
//        // и вы будете вызывать foreach (var handler in handlers) { await handler.HandleAsync(...) }
//        // Для простоты примера оставим текущую логику вызова одного хендлера.
//        // Чтобы она заработала с несколькими, нужно изменить вызов в ExecuteAsync на:
//        // await Task.WhenAll(_handlers.Select(h => h.HandleAsync(quote, stoppingToken)));
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("📈 Генератор котировок запущен.");

//        DateTime nextRun = DateTime.UtcNow;

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var quotesBatch = GenerateQuotesBatch();
//            _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) ---");

//            // --- ИЗМЕНЕНИЕ ДЛЯ НЕСКОЛЬКИХ ХЕНДЛЕРОВ ---
//            // Если вы внедрили IEnumerable<IQuoteHandler> _handlers;
//            // await Task.WhenAll(_handlers.Select(h => h.HandleAsync(quote, stoppingToken)));

//            // Если у вас один хендлер:
//            await _handler.HandleAsync(quotesBatch[0], stoppingToken);

//            nextRun = nextRun.AddSeconds(_intervalSeconds);
//            var delayTask = Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);

//            await Task.WhenAny(delayTask, Task.Delay(Timeout.Infinite, stoppingToken));
//            if (delayTask.IsCompletedSuccessfully)
//                continue;
//            else
//                break;
//        }

//        _logger.LogInformation("🛑 Генератор котировок остановлен.");
//    }

//    // ... остальной код GenerateQuotesBatch и т.д. остается без изменений.

//    private async Task GenerateQuotesToQueueAsync(ChannelWriter<StockQuote> writer, CancellationToken stoppingToken)
//    {
//        try
//        {
//            DateTime nextRun = DateTime.UtcNow; // Время следующего запуска

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                // Генерируем пачку котировок
//                var quotesBatch = GenerateQuotesBatch();
//                _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) добавлена в очередь ---");

//                // Пытаемся положить каждую котировку в очередь
//                foreach (var quote in quotesBatch)
//                {
//                    if (writer.TryWrite(quote))
//                    {
//                        continue;
//                    }
//                    // Если очередь переполнена, можно подождать или отбросить элемент
//                    // Здесь мы ждем немного и пробуем снова, но можно сделать и break;
//                    await writer.WaitToWriteAsync(stoppingToken);
//                    writer.TryWrite(quote); // Пробуем записать снова
//                }

//                // --- КОРРЕКТНАЯ ЗАДЕРЖКА БЕЗ ВЫБРОСА ИСКЛЮЧЕНИЯ ---
//                nextRun = nextRun.AddSeconds(2);
//                var delayTask = Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);

//                await Task.WhenAny(delayTask, Task.Delay(Timeout.Infinite, stoppingToken));
//                if (delayTask.IsCompletedSuccessfully)
//                {
//                    continue;
//                }
//                else
//                {
//                    // Если задержка была прервана токеном, выходим из цикла
//                    break;
//                }
//            }
//        }
//        finally
//        {
//            // Сигнализируем потребителю, что новых котировок больше не будет
//            writer.Complete();
//            _logger.LogDebug("--- Производитель завершил работу. Очередь закрыта для записи. ---");
//        }
//    }
//}

//public class QuoteGeneratorService : BackgroundService
//{
//    private readonly IQuoteHandler _handler;
//    // Используем массив для фиксированного списка символов (эффективно)
//    private readonly string[] _symbols = { "GAZP", "SBER", "LKOH", "ROSN", "YNDX", "TCSG", "PLZL", "GMKN", "MOEX", "AFKS" };
//    private readonly Random _random = new();
//    private readonly ILogger<QuoteGeneratorService> _logger;

//    // Канал выступает в роли нашей очереди
//    private readonly Channel<StockQuote> _channel;

//    public QuoteGeneratorService(IQuoteHandler handler, ILogger<QuoteGeneratorService> logger)
//    {
//        _handler = handler;
//        _logger = logger;
//        // Создаем ограниченный канал (очередь). Например, на 100 элементов.
//        // SingleWriter=true оптимизирует производительность, так как писать будет только один поток.
//        _channel = Channel.CreateBounded<StockQuote>(new BoundedChannelOptions(100) { SingleWriter = true });
//    }

//    // Внутри класса QuoteGeneratorService

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("📈 Генератор котировок запущен.");

//        // Запускаем потребителя (обработчика), который будет читать из очереди
//        var consumerTask = ProcessQuotesFromQueueAsync(_channel.Reader, stoppingToken);

//        // Запускаем производителя (поставщика), который будет генерировать котировки
//        await GenerateQuotesToQueueAsync(_channel.Writer, stoppingToken);

//        // --- ИЗМЕНЕННАЯ ЧАСТЬ ---
//        // Ждем завершения обработчика, но перехватываем ожидаемое исключение
//        try
//        {
//            await consumerTask;
//            _logger.LogDebug("--- Потребитель успешно завершил обработку очереди. ---");
//        }
//        catch (OperationCanceledException)
//        {
//            // Это исключение выбрасывает ReadAllAsync при отмене.
//            // Мы его здесь "гасим", так как это нормальная часть процесса остановки.
//            _logger.LogDebug("--- Получен сигнал отмены. Потребитель завершил работу. ---");
//        }
//        // -----------------------

//        _logger.LogInformation("🛑 Генератор котировок остановлен.");
//    }


//    /// <summary>
//    #region Producer: Генерирует котировки и кладет их в очередь
//    private async Task GenerateQuotesToQueueAsync(ChannelWriter<StockQuote> writer, CancellationToken stoppingToken)
//    {
//        try
//        {
//            DateTime nextRun = DateTime.UtcNow; // Время следующего запуска

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                // Генерируем пачку котировок
//                var quotesBatch = GenerateQuotesBatch();
//                _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) добавлена в очередь ---");

//                // Пытаемся положить каждую котировку в очередь
//                foreach (var quote in quotesBatch)
//                {
//                    if (writer.TryWrite(quote))
//                    {
//                        continue;
//                    }
//                    // Если очередь переполнена, можно подождать или отбросить элемент
//                    // Здесь мы ждем немного и пробуем снова, но можно сделать и break;
//                    await writer.WaitToWriteAsync(stoppingToken);
//                    writer.TryWrite(quote); // Пробуем записать снова
//                }

//                // --- КОРРЕКТНАЯ ЗАДЕРЖКА БЕЗ ВЫБРОСА ИСКЛЮЧЕНИЯ ---
//                nextRun = nextRun.AddSeconds(2);
//                var delayTask = Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);

//                await Task.WhenAny(delayTask, Task.Delay(Timeout.Infinite, stoppingToken));
//                if (delayTask.IsCompletedSuccessfully)
//                {
//                    continue;
//                }
//                else
//                {
//                    // Если задержка была прервана токеном, выходим из цикла
//                    break;
//                }
//            }
//        }
//        finally
//        {
//            // Сигнализируем потребителю, что новых котировок больше не будет
//            writer.Complete();
//            _logger.LogDebug("--- Производитель завершил работу. Очередь закрыта для записи. ---");
//        }
//    }
//    #endregion

//    /// <summary>
//    #region Consumer: Читает котировки из очереди и обрабатывает их
//    private async Task ProcessQuotesFromQueueAsync(ChannelReader<StockQuote> reader, CancellationToken stoppingToken)
//    {
//        await foreach (var quote in reader.ReadAllAsync(stoppingToken))
//        {
//            await _handler.HandleAsync(quote, stoppingToken);
//        }
//        _logger.LogDebug("--- Потребитель обработал все котировки из очереди. ---");
//    }
//    #endregion

//    #region Вспомогательные методы
//    private List<StockQuote> GenerateQuotesBatch()
//    {
//        return _symbols.Select(symbol => new StockQuote(
//                symbol,
//                GenerateRandomPrice()
//            )).ToList();
//    }

//    private decimal GenerateRandomPrice()
//    {
//        var offset = _random.Next(-2000, 2001) / 100m;
//        return Math.Round(150m + offset, 2);
//    }
//    #endregion
//}