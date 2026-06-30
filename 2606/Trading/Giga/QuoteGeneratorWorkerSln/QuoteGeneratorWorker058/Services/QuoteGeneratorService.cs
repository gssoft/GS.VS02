using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuoteGeneratorWorker.Models;
using QuoteGeneratorWorker.Options;

namespace QuoteGeneratorWorker.Services
{
    public class QuoteGeneratorService : BackgroundService
    {
        private readonly ILogger<QuoteGeneratorService> _logger;
        private readonly string[] _symbols;
        private readonly Timer _generationTimer;
        private readonly Random _random = new();
        private int _currentSymbolIndex = -1;

        // Монитор для BroadcastBlock
        private readonly BlockMonitor _broadcastMonitor;
        // Список мониторов для ActionBlock-потребителей
        private readonly List<BlockMonitor> _consumerMonitors = new();

        // Сам BroadcastBlock
        private readonly BroadcastBlock<StockQuote> _broadcastBlock;

        public QuoteGeneratorService(
            ILogger<QuoteGeneratorService> logger,
            IOptions<StockQuoteOptions> options)
        {
            _logger = logger;
            _symbols = options.Value.Symbols.ToArray();
            int intervalMs = options.Value.IntervalSeconds * 1000;

            // 1. Создаём BroadcastBlock и его монитор
            _broadcastBlock = new BroadcastBlock<StockQuote>(quote => quote);
            _broadcastMonitor = new BlockMonitor
            {
                Block = _broadcastBlock,
                Name = "BroadcastBlock"
            };

            // 2. Создаём потребителей с мониторами

            // a) ApiHandler – последовательный
            var apiMonitor = new BlockMonitor { Block = null!, Name = "ApiHandler" };
            var apiHandler = new ActionBlock<StockQuote>(CreateHandlerDelegate(apiMonitor, async quote =>
            {
                await Task.Delay(50);
                _logger.LogInformation("ApiHandler: Отправка котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }));
            apiMonitor.Block = apiHandler;
            _consumerMonitors.Add(apiMonitor);

            // b) DatabaseHandler – параллельный
            var dbMonitor = new BlockMonitor { Block = null!, Name = "DatabaseHandler" };
            var dbHandler = new ActionBlock<StockQuote>(CreateHandlerDelegate(dbMonitor, async quote =>
            {
                await Task.Delay(50);
                _logger.LogInformation("DatabaseHandler: Сохранение котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });
            dbMonitor.Block = dbHandler;
            _consumerMonitors.Add(dbMonitor);

            // c) ConsoleHandler – последовательный
            var consoleMonitor = new BlockMonitor { Block = null!, Name = "ConsoleHandler" };
            var consoleHandler = new ActionBlock<StockQuote>(CreateHandlerDelegate(consoleMonitor, async quote =>
            {
                await Task.Delay(50);
                _logger.LogInformation("ConsoleHandler: Обработана котировка: {Symbol} - ${Price}", quote.Symbol, quote.Price);
            }));
            consoleMonitor.Block = consoleHandler;
            _consumerMonitors.Add(consoleMonitor);

            // 3. Подписываем потребителей на BroadcastBlock
            _broadcastBlock.LinkTo(apiHandler);
            _broadcastBlock.LinkTo(dbHandler);
            _broadcastBlock.LinkTo(consoleHandler);

            // 4. Таймер генерации
            _generationTimer = new Timer(GenerateAndPostQuote, null, Timeout.Infinite, intervalMs);
        }

        /// <summary>
        /// Фабрика делегата для ActionBlock, который автоматически увеличивает счётчики монитора.
        /// </summary>
        private Func<StockQuote, Task> CreateHandlerDelegate(BlockMonitor monitor, Func<StockQuote, Task> handler)
        {
            return async quote =>
            {
                // Увеличиваем счётчик принятых
                monitor.IncrementItemsReceived();

                try
                {
                    // Выполняем основную логику
                    await handler(quote);
                    // Успешно обработано
                    monitor.IncrementItemsProcessed();
                }
                catch (Exception ex)
                {
                    // Ошибка обработки
                    monitor.IncrementErrorsOccurred();
                    _logger.LogError(ex, "Ошибка при обработке котировки в блоке {BlockName}", monitor.Name);
                    // Не перебрасываем, чтобы не останавливать блок
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");
            _generationTimer.Change(0, Timeout.Infinite);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("--- Получен сигнал отмены. ---");
            }
        }

        private void GenerateAndPostQuote(object? state)
        {
            var quote = GenerateNextQuote();
            if (quote != null)
            {
                if (!_broadcastBlock.Post(quote))
                {
                    _logger.LogWarning("Не удалось отправить котировку в поток. Буфер переполнен.");
                }
                else
                {
                    // Увеличиваем счётчик отправленных для BroadcastBlock
                    _broadcastMonitor.IncrementItemsReceived();
                }
                _generationTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
            }
        }

        private StockQuote? GenerateNextQuote()
        {
            if (_symbols.Length == 0) return null;

            _currentSymbolIndex = (_currentSymbolIndex + 1) % _symbols.Length;
            var symbol = _symbols[_currentSymbolIndex];

            var offset = _random.Next(-2000, 2001) / 100m;
            var price = Math.Round(150m + offset, 2);

            return new StockQuote(symbol, price);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Инициирована остановка генератора котировок.");

            // 1. Останавливаем таймер
            _logger.LogInformation("🛑 1. Останавливаем генерацию новых котировок.");
            _generationTimer?.Change(Timeout.Infinite, 0);

            // 2. Завершаем все блоки-потребители
            _logger.LogInformation("🛑 2. Завершаем все блоки-потребители.");
            foreach (var monitor in _consumerMonitors)
            {
                _logger.LogInformation("🛑 2. Завершаем блок-потребитель: {BlockName}", monitor.Name);
                monitor.Block.Complete();
            }

            // 3. Ждём завершения всех потребителей
            _logger.LogInformation("🛑 3. Ожидаем завершения всех обработчиков.");
            await Task.WhenAll(_consumerMonitors.Select(m => m.Block.Completion));

            // 4. Завершаем BroadcastBlock
            _logger.LogInformation("🛑 4. Завершаем {BlockName}.", _broadcastMonitor.Name);
            _broadcastBlock.Complete();

            // 5. Выводим статистику по всем блокам
            _logger.LogInformation("📊 Статистика работы блоков:");
            LogStatistics(_broadcastMonitor);
            foreach (var monitor in _consumerMonitors)
            {
                LogStatistics(monitor);
            }

            _logger.LogInformation("🛑 5. Генератор котировок остановлен.");
        }

        private void LogStatistics(BlockMonitor monitor)
        {
            var elapsed = DateTime.UtcNow - monitor.StartTimeUtc;
            _logger.LogInformation(
                "Блок {Name}: Принято = {Received}, Обработано = {Processed}, Ошибок = {Errors}, Время работы = {Elapsed:c}",
                monitor.Name,
                monitor.ItemsReceived,
                monitor.ItemsProcessed,
                monitor.ErrorsOccurred,
                elapsed);
        }

        // =========================== Внутренний класс монитора ===========================
        private class BlockMonitor
        {
            // Ссылка на блок (может быть IDataflowBlock, но для удобства храним как IDataflowBlock)
            public required IDataflowBlock Block { get; set; }

            public required string Name { get; init; }

            // Счётчики
            private long _itemsReceived;
            private long _itemsProcessed;
            private long _errorsOccurred;

            public long ItemsReceived => Interlocked.Read(ref _itemsReceived);
            public long ItemsProcessed => Interlocked.Read(ref _itemsProcessed);
            public long ErrorsOccurred => Interlocked.Read(ref _errorsOccurred);

            public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

            internal void IncrementItemsReceived() => Interlocked.Increment(ref _itemsReceived);
            internal void IncrementItemsProcessed() => Interlocked.Increment(ref _itemsProcessed);
            internal void IncrementErrorsOccurred() => Interlocked.Increment(ref _errorsOccurred);
        }
    }
}