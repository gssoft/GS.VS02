// Поместите этот файл в папку Services (создайте её, если нет)
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuoteGeneratorWorker.Handlers;
using QuoteGeneratorWorker.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteGeneratorWorker.Services
{
    public class QuoteGeneratorService : BackgroundService
    {
        private readonly IQuoteHandler _handler;
        // Используем массив для фиксированного списка символов (эффективно)
        private readonly string[] _symbols = { "GAZP", "SBER", "LKOH", "ROSN", "YNDX", "TCSG", "PLZL", "GMKN", "MOEX", "AFKS" };
        private readonly Random _random = new();
        private readonly ILogger<QuoteGeneratorService> _logger;

        public QuoteGeneratorService(IQuoteHandler handler, ILogger<QuoteGeneratorService> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        /// <summary>
        /// Вызывается при запуске приложения. Идеальное место для инициализации.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("📈 Генератор котировок готовится к запуску...");

            // Здесь можно было бы подключиться к базе данных или другому внешнему сервису.
            // await InitializeDatabaseConnectionAsync(cancellationToken);

            _logger.LogInformation("🚀 Инициализация завершена. Запуск основного цикла генерации.");

            // Обязательно вызываем базовую реализацию,
            // которая запустит наш основной метод ExecuteAsync
            await base.StartAsync(cancellationToken);
        }
        /// <summary>
        /// Основной рабочий цикл сервиса. Должен завершаться, когда stoppingToken.IsCancellationRequested станет true.
        /// Содержит только бизнес-логику работы.
        /// </summary>
        /// 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");

            DateTime nextRun = DateTime.UtcNow; // Время следующего запуска

            while (!stoppingToken.IsCancellationRequested)
            {
                var quotesBatch = GenerateQuotesBatch();
                _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) ---");

                await Parallel.ForEachAsync(
                    quotesBatch,
                    new ParallelOptions { CancellationToken = stoppingToken },
                    async (quote, token) => await _handler.HandleAsync(quote, token));

                // --- КОРРЕКТНАЯ ЗАДЕРЖКА БЕЗ ВЫБРОСА ИСКЛЮЧЕНИЯ ---

                // Вычисляем время следующего такта (через 2 секунды от предыдущего)
                nextRun = nextRun.AddSeconds(2);

                // Создаем задачу для запланированного времени
                Task delayTask = Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);

                try
                {
                    // Ждем либо окончания задержки, либо сигнала отмены
                    await Task.WhenAny(delayTask, Task.Delay(Timeout.Infinite, stoppingToken));

                    // Если задержка завершилась успешно, просто идем на следующую итерацию
                    if (delayTask.IsCompletedSuccessfully)
                    {
                        continue;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Исключение может быть выброшено изнутри Task.Delay.
                    // Мы его ловим здесь, чтобы предотвратить "взрыв" приложения.
                    // Это штатная ситуация при остановке, поэтому просто выходим из цикла.
                    break;
                }
            }

            _logger.LogDebug("--- Цикл обработки завершен по запросу ---");
        }

        /// <summary>
        /// Вызывается при остановке приложения. Идеальное место для корректного завершения и очистки ресурсов.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("⚠️ Получен сигнал об остановке сервиса. Начинаю процедуру завершения...");

            // Здесь выполняется логика очистки:
            // - Сохранение финального состояния
            // - Закрытие соединений
            // - Финальное логирование

            _logger.LogInformation("🛑 Сервис успешно остановился. Все ресурсы освобождены.");

            // Обязательно вызываем базовую реализацию
            await base.StopAsync(cancellationToken);
        }

        #region Вспомогательные методы

        /// <summary>
        /// Генерирует пачку котировок для всех символов.
        /// Логика генерации цен изолирована здесь.
        /// </summary>
        private List<StockQuote> GenerateQuotesBatch()
        {
            return _symbols.Select(symbol => new StockQuote(
                symbol,
                GenerateRandomPrice()
            )).ToList();
        }

        /// <summary>
        /// Генерирует одну случайную цену от 130 до 170.
        /// Использует целочисленную арифметику для точности decimal.
        /// </summary>
        private decimal GenerateRandomPrice()
        {
            // Генерация целого числа от -2000 до 2000 дает нам диапазон смещения [-20.00, 20.00]
            var offset = _random.Next(-2000, 2001) / 100m;
            return Math.Round(150m + offset, 2);
        }

        #endregion
    }
}
