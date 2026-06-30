using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow; // <-- ВАЖНО: Добавить этот using
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

        // Ядро системы - блок, который рассылает данные всем подписчикам.
        private readonly BroadcastBlock<StockQuote> _broadcastBlock;
        // Список блоков-обработчиков для корректной остановки сервиса.
        private readonly List<ActionBlock<StockQuote>> _consumerBlocks = new();

        public QuoteGeneratorService(
            ILogger<QuoteGeneratorService> logger,
            IOptions<StockQuoteOptions> options)
        {
            _logger = logger;
            _symbols = options.Value.Symbols.ToArray();
            int intervalMs = options.Value.IntervalSeconds * 1000; // Переводим в миллисекунды для Timer

            // Создаем BroadcastBlock. Он будет клонировать входящую котировку для всех подписчиков.
            _broadcastBlock = new BroadcastBlock<StockQuote>(quote => quote);

            // --- Настройка и создание ActionBlocks (потребителей) ---
            // Блок для имитации отправки по API. Работает строго последовательно.
            var apiHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50); // Имитация сетевой задержки
                _logger.LogInformation("ApiHandler: Отправка котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            // Блок для имитации записи в БД. Может работать параллельно.
            var dbHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50); // Имитация работы с БД
                _logger.LogInformation("DatabaseHandler: Сохранение котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

            // Блок для вывода в консоль.
            var consoleHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50);
                _logger.LogInformation("ConsoleHandler: Обработана котировка: {Symbol} - ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            // Сохраняем блоки в список для управления их завершением.
            _consumerBlocks.Add(apiHandler);
            _consumerBlocks.Add(dbHandler);
            _consumerBlocks.Add(consoleHandler);

            // Подписываем обработчики на BroadcastBlock.
            _broadcastBlock.LinkTo(apiHandler);
            _broadcastBlock.LinkTo(dbHandler);
            _broadcastBlock.LinkTo(consoleHandler);

            // Создаем таймер, который будет вызывать метод генерации котировок.
            _generationTimer = new Timer(GenerateAndPostQuote, null, Timeout.Infinite, intervalMs);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");

            // Запускаем таймер генерации котировок.
            _generationTimer.Change(0, Timeout.Infinite);

            try
            {
                // Ждем сигнала отмены (например, Ctrl+C)
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при остановке сервиса.
                _logger.LogDebug("--- Получен сигнал отмены. ---");
            }
        }

        private void GenerateAndPostQuote(object? state)
        {
            var quote = GenerateNextQuote();
            if (quote != null)
            {
                // Post возвращает false, если буфер блока полон и не может принять сообщение.
                if (!_broadcastBlock.Post(quote))
                {
                    _logger.LogWarning("Не удалось отправить котировку в поток. Буфер переполнен.");
                }
                // Перезапускаем таймер для следующего такта.
                _generationTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
            }
        }

        private StockQuote? GenerateNextQuote()
        {
            if (_symbols.Length == 0) return null;

            _currentSymbolIndex = (_currentSymbolIndex + 1) % _symbols.Length;
            var symbol = _symbols[_currentSymbolIndex];

            var offset = _random.Next(-2000, 2001) / 100m; // Смещение от -20.00 до 20.00
            var price = Math.Round(150m + offset, 2);

            return new StockQuote(symbol, price);
        }
        // Вариант похожий на Deepseek c комментариями тоже не работает
        // Work Well Deepseek 26.06.30
        //public override async Task StopAsync(CancellationToken cancellationToken)
        //{
        //    _logger.LogInformation("🛑 Инициирована остановка генератора котировок.");

        //    // 1. Останавливаем генерацию новых котировок.
        //    // Это предотвращает попадание новых данных в систему.
        //    _logger.LogInformation("🛑 1. Останавливаем генерацию новых котировок.");
        //    _generationTimer?.Change(Timeout.Infinite, 0);

        //    // 2. Сообщаем BroadcastBlock, что новых данных не будет.
        //    // Он перестанет принимать сообщения и передаст статус "Complete" всем подписчикам.
        //    _logger.LogInformation("🛑 2. Сообщаем BroadcastBlock, что новых данных не будет.");
        //    _broadcastBlock.Complete();

        //    // 3. Ждем завершения всех обработчиков (ActionBlocks).
        //    // Task.WhenAll позволяет дождаться, когда все задачи (завершение каждого блока)
        //    // будут выполнены. Это асинхронная операция, которая не блокирует поток.
        //    _logger.LogInformation("🛑 3. Ждем завершения всех обработчиков (ActionBlocks).");
        //    await Task.WhenAll(_consumerBlocks.Select(block => block.Completion));

        //    // 4. Освобождаем ресурсы таймера.
        //    _logger.LogInformation("🛑 4. Освобождаем ресурсы таймера.");
        //    _generationTimer?.Dispose();

        //    _logger.LogInformation("🛑 Генератор котировок остановлен.");
        //}

        // Work Well Deepseek 26.06.30
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Инициирована остановка генератора котировок.");

            // 1. Останавливаем генерацию новых котировок.
            _logger.LogInformation("🛑 1. Останавливаем генерацию новых котировок.");
            _generationTimer?.Change(Timeout.Infinite, 0);

            // 2. Завершаем все блоки-потребители.
            _logger.LogInformation("🛑 2. Завершаем все блоки-потребители");
            foreach (var block in _consumerBlocks)
            {
                block.Complete();
            }
            // 3. Ждём завершения всех обработчиков (ActionBlocks).
            _logger.LogInformation("🛑 3. Ждём завершения всех обработчиков (ActionBlocks)");
            await Task.WhenAll(_consumerBlocks.Select(b => b.Completion));

            // 4. Завершаем BroadcastBlock (он уже не нужен).
            _logger.LogInformation("🛑 4. Завершаем BroadcastBlock (он уже не нужен).");
            _broadcastBlock.Complete();

            _logger.LogInformation("🛑 5. Генератор котировок остановлен.");
        }
    }
}
