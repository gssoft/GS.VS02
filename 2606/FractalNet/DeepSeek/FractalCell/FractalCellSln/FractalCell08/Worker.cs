using FractalCell.Core.Interfaces;
using FractalCell.Core;
using FractalCell.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.JavaScript;

namespace FractalCell;

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
        _logger.LogInformation("🚀 Worker (Orchestrator) started");

        try
        {
            await InitializeSystemAsync(stoppingToken);

            _logger.LogInformation("✅ System initialized. Starting orchestration loop...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await OrchestrateAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("⏹️ Orchestration loop canceled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in orchestration loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("👋 Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical worker error");
        }
        finally
        {
            _logger.LogInformation("🏁 Worker (Orchestrator) finished");
        }
    }

    private async Task InitializeSystemAsync(CancellationToken ct)
    {
        _logger.LogInformation("🏗️ Initializing fractal system...");

        var rootCell = await CreateCellAsync("Root", 3, ct);
        _cells.Add(rootCell);

        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
        foreach (var childId in childCells)
        {
            var child = await CreateCellAsync(childId, 2, ct);
            _cells.Add(child);
        }

        _logger.LogInformation("🔍 System cells: {Count}", _cells.Count);
        foreach (var cell in _cells)
        {
            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
        }

        _logger.LogInformation("▶️ Starting all cells...");
        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

        _logger.LogInformation("✅ System initialized with {Count} cells", _cells.Count);
    }

    private async Task<IFractalCell> CreateCellAsync(
        string cellId,
        int workers,
        CancellationToken ct)
    {
        var config = new FractalCellConfiguration
        {
            CellId = cellId,
            BackgroundServiceCount = workers,
            InternalBusType = BusType.Channels,
            ExternalBusType = BusType.Channels,
            BusSettings = new BusSettings
            {
                Capacity = 1000,
                MaxParallelism = 4
            }
        };

        var cell = await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
        _logger.LogInformation("📦 Cell {CellId} created with {Workers} workers", cellId, workers);
        return cell;
    }

    private async Task OrchestrateAsync(CancellationToken ct)
    {
        if (_cells.Count == 0) return;

        var random = new Random();
        var rootCell = _cells.First();
        var targetCell = _cells[random.Next(_cells.Count)];

        var @event = new FractalEvent(
            $"heartbeat-{DateTime.UtcNow.Ticks}",
            DateTime.UtcNow,
            rootCell.CellId,
            targetCell.CellId,
            "Heartbeat",
            new
            {
                Timestamp = DateTime.UtcNow,
                Source = "Orchestrator",
                ActiveCells = _hub.GetActiveCells().Count
            }
        );

        _logger.LogInformation("📤 [ORCHESTRATOR] Sending heartbeat from {Source} to {Target}",
            rootCell.CellId, targetCell.CellId);

        await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Stopping all cells...");

        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

        _logger.LogInformation("✅ All cells stopped");
        await base.StopAsync(cancellationToken);
    }
}

//using FractalCell.Core;
//using FractalCell.Core.Configuration;
//using FractalCell.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System.Runtime.InteropServices.JavaScript;

//namespace FractalCell;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("🚀 Worker (Orchestrator) started");

//        try
//        {
//            await InitializeSystemAsync(stoppingToken);

//            _logger.LogInformation("✅ System initialized. Starting orchestration loop...");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await OrchestrateAsync(stoppingToken);
//                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Orchestration loop canceled");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in orchestration loop");
//                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Critical worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker (Orchestrator) finished");
//        }
//    }

//    private async Task InitializeSystemAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Initializing fractal system...");

//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 System cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ System initialized with {Count} cells", _cells.Count);
//    }

//    private async Task<IFractalCell> CreateCellAsync(
//        string cellId,
//        int workers,
//        CancellationToken ct)
//    {
//        var config = new FractalCellConfiguration
//        {
//            CellId = cellId,
//            BackgroundServiceCount = workers,
//            InternalBusType = BusType.Channels,
//            ExternalBusType = BusType.Channels,
//            BusSettings = new BusSettings
//            {
//                Capacity = 1000,
//                MaxParallelism = 4
//            }
//        };

//        var cell = await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
//        _logger.LogInformation("📦 Cell {CellId} created with {Workers} workers", cellId, workers);
//        return cell;
//    }

//    private async Task OrchestrateAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0) return;

//        var random = new Random();
//        var rootCell = _cells.First();
//        var targetCell = _cells[random.Next(_cells.Count)];

//        var @event = new FractalEvent(
//            $"heartbeat-{DateTime.UtcNow.Ticks}",
//            DateTime.UtcNow,
//            rootCell.CellId,
//            targetCell.CellId,
//            "Heartbeat",
//            new
//            {
//                Timestamp = DateTime.UtcNow,
//                Source = "Orchestrator",
//                ActiveCells = _hub.GetActiveCells().Count
//            }
//        );

//        _logger.LogInformation("📤 [ORCHESTRATOR] Sending heartbeat from {Source} to {Target}",
//            rootCell.CellId, targetCell.CellId);

//        await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}
