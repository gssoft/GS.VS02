// Поместите этот файл в папку Services (создайте её, если нет)
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using QuoteGeneratorWorker.Handlers;
using QuoteGeneratorWorker.Models;

namespace QuoteGeneratorWorker.Services;

public class QuoteGeneratorService : BackgroundService
{
    private readonly IQuoteHandler _handler;
    private readonly string[] _symbols = { "GAZP", "SBER", "LKOH", "ROSN", "YNDX", "TCSG", "PLZL", "GMKN", "MOEX", "AFKS" };
    private readonly Random _random = new();
    private readonly ILogger<QuoteGeneratorService> _logger;

    public QuoteGeneratorService(IQuoteHandler handler, ILogger<QuoteGeneratorService> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool isShuttingDown = false;
        try
        {
            _logger.LogInformation("📈 Генератор котировок запущен.");
            while (!stoppingToken.IsCancellationRequested || !isShuttingDown)
            {
                if (!isShuttingDown)
                {
                    var quotesBatch = GenerateQuotesBatch();
                    _logger.LogDebug($"--- Новая порция котировок ({quotesBatch.Count} шт.) ---");
                    using var processCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    try
                    {
                        await Parallel.ForEachAsync(
                            quotesBatch,
                            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = processCts.Token },
                            async (quote, token) => await _handler.HandleAsync(quote, token));
                    }
                    catch (OperationCanceledException) when (processCts.IsCancellationRequested)
                    {
                        _logger.LogWarning("Processing of a batch of quotes was cancelled.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while processing a batch of quotes.");
                    }
                }
                if (stoppingToken.IsCancellationRequested && !isShuttingDown)
                {
                    _logger.LogInformation("🛑 Signal received to stop the service after current batch.");
                    isShuttingDown = true;
                    await Task.Delay(TimeSpan.FromMilliseconds(100));  // timeer was = 100 
                    continue;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)); // // timeer was = 1
            }
            _logger.LogInformation("🛑 Генератор котировок остановлен.");
        }
        finally
        {
            _logger.LogInformation("Сервис QuoteGeneratorService полностью остановлен.");
        }
    }
    // --- НОВАЯ РЕАЛИЗАЦИЯ СТАРТА ---
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("📈 Генератор котировок готовится к запуску...");

        // Здесь может быть логика инициализации, например:
        // await InitializeDatabaseConnectionAsync(cancellationToken);

        _logger.LogInformation("🚀 Инициализация завершена. Запуск основного цикла.");

        // Вызываем стандартный старт, который начнет ExecuteAsync
        await base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("⚠️ Получен сигнал об остановке сервиса.");

        // Здесь может быть логика очистки, например:
        // await SaveFinalStateToDatabaseAsync(cancellationToken);
        // CloseFileHandles();

        // _logger.LogInformation("🛑 Сервис успешно остановлен.");

        // Вызываем стандартную остановку
        await base.StopAsync(cancellationToken);
    }

    private List<StockQuote> GenerateQuotesBatch()
    {
        // Локальная функция для генерации одной цены.
        // Использует Random.Next(int, int) для работы с целыми числами,
        // что точнее для финансовых расчетов, чем NextDouble().
        decimal GeneratePrice()
        {
            // Генерируем целое число от -2000 до 2000 (всего 4001 возможное значение).
            // Делим на 100, чтобы получить смещение от -20.00 до 20.00.
            var offset = _random.Next(-2000, 2001) / 100m;
            return Math.Round(150m + offset, 2);
        }

        // Создаем список котировок, применяя функцию GeneratePrice к каждому символу.
        return _symbols.Select(symbol => new StockQuote(
                symbol,
                GeneratePrice() // <-- ИСПРАВЛЕННАЯ СТРОКА
            )).ToList();
    }
}
