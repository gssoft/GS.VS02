
// File: Services/QuoteGeneratorService.cs

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
    /// <summary>
    /// Сервис, генерирующий котировки акций и обрабатывающий их с помощью TPL Dataflow.
    /// </summary>
    public class QuoteGeneratorService : BackgroundService
    {
        private readonly ILogger<QuoteGeneratorService> _logger;
        private readonly string[] _symbols;
        private readonly Random _random = new();
        private int _currentSymbolIndex = -1;

        // --- Блоки TPL Dataflow ---
        private readonly BroadcastBlock<StockQuote> _broadcastBlock;
        private readonly List<ActionBlock<StockQuote>> _consumerBlocks = new();

        // --- Компоненты для генерации и остановки ---
        private Timer? _generationTimer;
        private CancellationTokenSource? _internalCts; // Для внутренней отмены операций

        public QuoteGeneratorService(
            ILogger<QuoteGeneratorService> logger,
            IOptions<StockQuoteOptions> options)
        {
            _logger = logger;
            _symbols = options.Value.Symbols.ToArray();

            // 1. Создаем BroadcastBlock - ядро системы.
            // Он будет клонировать входящую котировку для всех подписчиков.
            _broadcastBlock = new BroadcastBlock<StockQuote>(quote => quote);

            // 2. Создаем ActionBlocks (потребителей) с настройками параллелизма.
            // API Handler: Последовательная обработка (MaxDegreeOfParallelism = 1)
            var apiHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50); // Имитация сетевой задержки
                _logger.LogInformation("ApiHandler: Отправка котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            // Database Handler: Параллельная обработка (например, 4 потока)
            var dbHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50); // Имитация работы с БД
                _logger.LogInformation("DatabaseHandler: Сохранение котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

            // Console Handler: Последовательная обработка
            var consoleHandler = new ActionBlock<StockQuote>(async quote =>
            {
                await Task.Delay(50);
                _logger.LogInformation("ConsoleHandler: Обработана котировка: {Symbol} - ${Price}", quote.Symbol, quote.Price);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

            // Сохраняем блоки для управления их завершением
            _consumerBlocks.Add(apiHandler);
            _consumerBlocks.Add(dbHandler);
            _consumerBlocks.Add(consoleHandler);

            // 3. Связываем BroadcastBlock с ActionBlocks (подписываем потребителей)
            _broadcastBlock.LinkTo(apiHandler);
            _broadcastBlock.LinkTo(dbHandler);
            _broadcastBlock.LinkTo(consoleHandler);
        }

        /// <summary>
        /// Основной метод сервиса, запускаемый при старте приложения.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");

            // Создаем внутренний источник отмены, связанный с внешним (stoppingToken).
            // Это позволяет нам отменить внутренние операции из метода StopAsync.
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            // Запускаем таймер, который будет вызывать метод генерации котировок.
            // Интервал зададим здесь для ясности.
            _generationTimer = new Timer(GenerateAndPostQuote, _internalCts.Token, 0, Timeout.Infinite);

            try
            {
                // Ожидаем сигнала отмены от хоста (например, Ctrl+C).
                // Этот Task.Delay завершится, когда stoppingToken будет отменен.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при остановке сервиса.
                _logger.LogDebug("--- Получен внешний сигнал отмены. ---");
                // Исключение проглатывается, так как остановка - это штатная процедура.
                // Выполнение продолжится в блоке finally.
            }
        }

        /// <summary>
        /// Метод, вызываемый таймером для генерации и отправки котировки.
        /// </summary>
        private void GenerateAndPostQuote(object? state)
        {
            // Получаем CancellationToken из объекта state, переданного в Timer.
            var token = (CancellationToken)state!;

            // Если отмена уже запрошена, выходим немедленно.
            if (token.IsCancellationRequested)
                return;

            var quote = GenerateNextQuote();

            if (quote != null)
            {
                // Post возвращает false, если буфер блока полон.
                if (!_broadcastBlock.Post(quote))
                {
                    _logger.LogWarning("Не удалось отправить котировку в поток. Буфер переполнен.");
                }

                // Планируем следующий запуск таймера, только если отмена не была запрошена.
                // Это предотвращает "фантомный" запуск таймера после команды на остановку.
                if (!token.IsCancellationRequested)
                {
                    _generationTimer!.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
                }
            }
        }

        private StockQuote? GenerateNextQuote()
        {
            if (_symbols.Length == 0) return null;

            _currentSymbolIndex = (_currentSymbolIndex + 1) % _symbols.Length;
            var symbol = _symbols[_currentSymbolIndex];

            // Генерация цены в диапазоне ~130 - 170
            var offset = _random.Next(-2000, 2001) / 100m; // от -20.00 до 20.00
            var price = Math.Round(150m + offset, 2);

            return new StockQuote(symbol, price);
        }

        /// <summary>
        /// Метод, вызываемый при остановке приложения (Ctrl+C).
        /// Гарантирует корректное завершение всех асинхронных операций.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Инициирована остановка генератора котировок.");

            // 1. Запрашиваем внутреннюю отмену операций.
            // Это остановит логику в GenerateAndPostQuote и отменит Task.Delay в ExecuteAsync.
            _internalCts?.Cancel();

            // 2. Останавливаем таймер генерации.
            // Меняем интервал на бесконечный, чтобы остановить тики.
            _generationTimer?.Change(Timeout.Infinite, 0);

            // 3. Сообщаем BroadcastBlock, что новых данных не будет.
            // Он перестанет принимать новые сообщения и передаст статус "Complete" подписчикам.
            _broadcastBlock.Complete();

            // 4. Ждем завершения всех обработчиков (ActionBlocks).
            // Они завершат обработку оставшихся в буфере котировок и перейдут в статус Completed.
            await Task.WhenAll(_consumerBlocks.Select(b => b.Completion));

            // 5. Освобождаем ресурсы (необходимо для корректного завершения процесса).
            _generationTimer?.Dispose();
            _internalCts?.Dispose();

            _logger.LogInformation("🛑 Генератор котировок остановлен.");
        }
    }
}  