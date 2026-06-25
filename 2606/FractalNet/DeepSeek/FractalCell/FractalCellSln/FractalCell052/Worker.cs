using FractalCell02.Core;
using FractalCell02.Core.Behaviors;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Trading.Behaviors;
using FractalCell02.Trading.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FractalCell02;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFractalEventHub _hub;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<IFractalCell> _cells = new();

    public Worker(
        ILogger<Worker> logger,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _hub = hub;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Trading Orchestrator started");

        try
        {
            await InitializeTradingSystemAsync(stoppingToken);

            _logger.LogInformation("✅ Trading system initialized. Starting simulation...");

            // Даем системе время на запуск генератора котировок
            await Task.Delay(2000, stoppingToken);

            // Запускаем цикл отправки ордеров
            await RunOrderSimulationAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("👋 Orchestrator stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical orchestrator error");
        }
        finally
        {
            _logger.LogInformation("🏁 Orchestrator finished");
        }
    }

    private async Task InitializeTradingSystemAsync(CancellationToken ct)
    {
        _logger.LogInformation("🏗️ Building trading topology...");

        // ═══════════════════════════════════════════════
        // 1. Генератор котировок (Broadcasts QuoteEvents)
        // ═══════════════════════════════════════════════
        var quoteCell = await CreateCellAsync(
            cellId: "QuoteGen-AAPL",
            behavior: new QuoteGeneratorBehavior(
                symbol: "AAPL",
                startPrice: 185.00m,
                spread: 0.05m,
                intervalMs: 500),
            ct);

        // ═══════════════════════════════════════════════
        // 2. Matching Engine (Исполнитель ордеров)
        // ═══════════════════════════════════════════════
        var matchingEngineCell = await CreateCellAsync(
            cellId: "MatchingEngine-AAPL",
            behavior: new MatchingEngineBehavior(symbol: "AAPL"),
            ct);

        // ═══════════════════════════════════════════════
        // 3. Портфели клиентов
        // ═══════════════════════════════════════════════
        var portfolioA = await CreateCellAsync(
            cellId: "Portfolio-ClientA",
            behavior: new PortfolioBehavior(
                clientName: "Alice",
                initialCash: 100_000m,
                matchingEngineCellId: "MatchingEngine-AAPL"),
            ct);

        var portfolioB = await CreateCellAsync(
            cellId: "Portfolio-ClientB",
            behavior: new PortfolioBehavior(
                clientName: "Bob",
                initialCash: 250_000m,
                matchingEngineCellId: "MatchingEngine-AAPL"),
            ct);

        // Запускаем все ячейки
        _logger.LogInformation("▶️ Starting all trading cells...");
        foreach (var cell in _cells)
        {
            await cell.StartAsync(ct);
        }

        _logger.LogInformation(
            "✅ Trading topology: {Cells}",
            string.Join(" ↔ ", _cells.Select(c => c.CellId)));
    }

    /// <summary>
    /// Симуляция: оркестратор периодически отправляет ордера
    /// от имени клиентов в MatchingEngine
    /// </summary>
    private async Task RunOrderSimulationAsync(CancellationToken ct)
    {
        var random = new Random(42); // Детерминированный для воспроизводимости
        var orderNum = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);

                orderNum++;
                var isBuy = random.Next(2) == 0;
                var clientCellId = orderNum % 2 == 0
                    ? "Portfolio-ClientA"
                    : "Portfolio-ClientB";
                var clientName = orderNum % 2 == 0 ? "Alice" : "Bob";

                // Лимитная цена: ~185 ± 2
                var limitPrice = Math.Round(183m + (decimal)random.NextDouble() * 4m, 2);
                var quantity = (random.Next(5) + 1) * 10; // 10, 20, 30, 40, 50

                var order = new OrderEvent(
                    EventId: $"order-{Guid.NewGuid():N}"[..16],
                    Timestamp: DateTime.UtcNow,
                    OrderId: $"SIM-{orderNum:D4}",
                    ClientCellId: clientCellId,
                    Symbol: "AAPL",
                    Price: limitPrice,
                    Quantity: quantity,
                    Side: isBuy ? OrderSide.Buy : OrderSide.Sell,
                    OrderType: OrderType.Limit,
                    TargetCellId: "MatchingEngine-AAPL");

                _logger.LogInformation(
                    "📤 [ORCH] {Client} places order: {Side} {Qty} AAPL @ {Price} (Limit)",
                    clientName, order.Side, order.Quantity, order.Price);

                // Отправляем ордер в MatchingEngine
                await _hub.PublishAsync("MatchingEngine-AAPL", order);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in order simulation");
            }
        }
    }

    private async Task<IFractalCell> CreateCellAsync(
        string cellId,
        ICellBehavior behavior,
        CancellationToken ct)
    {
        var config = new FractalCellConfiguration
        {
            CellId = cellId,
            BackgroundServiceCount = 1,
            InternalBusType = BusType.Channels,
            ExternalBusType = BusType.Channels,
            BusSettings = new BusSettings
            {
                Capacity = 5000,
                MaxParallelism = 2
            }
        };

        var cell = await FractalCellFactory.CreateAsync(
            config, _hub, _loggerFactory, behavior, ct);

        _cells.Add(cell);

        _logger.LogInformation("📦 Cell {CellId} created ({Behavior})",
            cellId, behavior.GetType().Name);

        return cell;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Stopping all trading cells...");

        foreach (var cell in _cells)
        {
            await cell.StopAsync(cancellationToken);
        }

        _logger.LogInformation("✅ All trading cells stopped");
        await base.StopAsync(cancellationToken);
    }
}