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
        // private readonly IQuoteHandler _handler; // Текущая реализация использует один хендлер

        private readonly IEnumerable<IQuoteHandler> _handlers;

        private readonly string[] _symbols;
        private readonly int _intervalSeconds;
        private readonly Channel<StockQuote> _channel;
        private readonly Random _random = new();

        private int _currentSymbolIndex = -1; // <-- ДОБАВЬТЕ ЭТУ СТРОКУ

        public QuoteGeneratorService(
            IEnumerable<IQuoteHandler> handlers,
            ILogger<QuoteGeneratorService> logger,
            IOptions<StockQuoteOptions> options)
        {
            _handlers = handlers;
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
                    // ГЕНЕРИРУЕМ ТОЛЬКО ОДНУ КОТИРОВКУ ЗА РАЗ
                    var quote = GenerateNextQuote();
                    if (quote != null)
                    {
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
                // Запускаем хендлеры один за другим
                foreach (var handler in _handlers)
                {
                    await handler.HandleAsync(quote, stoppingToken);
                }
            }
        }
        #endregion

        #region Вспомогательные методы

        

        private StockQuote? GenerateNextQuote()
        {
            if (_symbols.Length == 0) return null;

            // Увеличиваем индекс и обнуляем его, если он превысил длину массива
            _currentSymbolIndex = (_currentSymbolIndex + 1) % _symbols.Length;

            var symbol = _symbols[_currentSymbolIndex];
            return new StockQuote(symbol, GenerateRandomPrice());
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