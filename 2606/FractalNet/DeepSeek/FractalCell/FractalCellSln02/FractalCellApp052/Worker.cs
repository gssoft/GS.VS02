// Worker.cs
using FractalCellApp.Behaviors;
using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.JavaScript;

namespace FractalCellApp;

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
            _logger.LogInformation("✅ System initialized. Worker is now idle, waiting for shutdown...");

            // Ожидаем сигнала остановки
            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("👋 Worker stopping due to cancellation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical worker error");
        }
        finally
        {
            _logger.LogInformation("🏁 Worker finished");
        }
    }

   

    // Worker.cs (ключевые изменения)

    private async Task InitializeSystemAsync(CancellationToken ct)
    {
        _logger.LogInformation("🏗️ Initializing fractal system with behaviors...");

        // === Корневая ячейка (два поведения) ===
        var rootBehaviors = new IBehavior[]
        {
        CreateHeartbeatBehavior(),
        CreateDataProcessingBehavior()
        };
        var rootCell = await CreateCellWithBehaviorsAsync("Root", 3, ct, rootBehaviors);
        _cells.Add(rootCell);

        // === Дочерние ячейки ===
        var childConfigs = new List<(string id, int workers, Func<IBehavior> behaviorFactory)>
    {
        ("Child-A", 2, CreateHeartbeatBehavior),
        ("Child-B", 2, CreateDataProcessingBehavior),
        ("Child-C", 1, CreateDataProcessingBehavior)
    };

        foreach (var (id, workers, behaviorFactory) in childConfigs)
        {
            var behavior = behaviorFactory();
            var child = await CreateCellWithBehaviorAsync(id, workers, ct, behavior);
            _cells.Add(child);
        }

        // === НОВОЕ: Ячейка-оркестратор ===
        var orchestratorBehavior = new OrchestratorBehavior(
            _loggerFactory.CreateLogger<OrchestratorBehavior>(),
            TimeSpan.FromSeconds(2) // интервал
        );
        var orchestratorCell = await CreateCellWithBehaviorAsync(
            "Orchestrator",
            1, // минимальное количество воркеров (можно и 0)
            ct,
            orchestratorBehavior
        );
        _cells.Add(orchestratorCell);

        _logger.LogInformation("🔍 System cells: {Count}", _cells.Count);
        foreach (var cell in _cells)
            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);

        _logger.LogInformation("▶️ Starting all cells...");
        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

        _logger.LogInformation("✅ System initialized with {Count} cells and behaviors", _cells.Count);
    }

    // Метод OrchestrateWithBehaviorsAsync полностью удалён (или закомментирован).
    // В ExecuteAsync больше нет вызова этого метода – оркестрация теперь внутри ячейки.

    // === Фабрики для создания поведений ===
    private IBehavior CreateHeartbeatBehavior()
    {
        var logger = _loggerFactory.CreateLogger<HeartbeatBehavior>();
        return new HeartbeatBehavior(logger, TimeSpan.FromSeconds(3));
    }

    private IBehavior CreateDataProcessingBehavior()
    {
        var logger = _loggerFactory.CreateLogger<DataProcessingBehavior>();
        return new DataProcessingBehavior(logger, 4);
    }

    // === Методы создания ячеек ===

    // Создание ячейки с одним поведением (существующий метод)
    private async Task<IFractalCell> CreateCellWithBehaviorAsync<TBehavior>(
        string cellId,
        int workers,
        CancellationToken ct,
        Type? behaviorType = null)
        where TBehavior : IBehavior, new()
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

        var cell = await FractalCellFactory.CreateWithBehaviorAsync<TBehavior>(
            config, _hub, _loggerFactory, ct: ct);

        _logger.LogInformation("📦 Cell {CellId} created with behavior {BehaviorName} and {Workers} workers",
            cellId, typeof(TBehavior).Name, workers);

        return cell;
    }

    // Создание ячейки с одним уже созданным поведением
    private async Task<IFractalCell> CreateCellWithBehaviorAsync(
        string cellId,
        int workers,
        CancellationToken ct,
        IBehavior behavior)
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

        var cell = await FractalCellFactory.CreateWithBehaviorsAsync(
            config, _hub, _loggerFactory, new[] { behavior }, ct);

        _logger.LogInformation("📦 Cell {CellId} created with behavior {BehaviorName} and {Workers} workers",
            cellId, behavior.BehaviorId, workers);

        return cell;
    }

    // === НОВЫЙ МЕТОД: Создание ячейки с несколькими поведениями ===
    private async Task<IFractalCell> CreateCellWithBehaviorsAsync(
        string cellId,
        int workers,
        CancellationToken ct,
        IEnumerable<IBehavior> behaviors)
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

        var cell = await FractalCellFactory.CreateWithBehaviorsAsync(
            config, _hub, _loggerFactory, behaviors, ct);

        var behaviorNames = string.Join(", ", behaviors.Select(b => b.BehaviorId));
        _logger.LogInformation("📦 Cell {CellId} created with behaviors [{BehaviorNames}] and {Workers} workers",
            cellId, behaviorNames, workers);

        return cell;
    }

    // === Оркестратор событий ===
    private async Task OrchestrateWithBehaviorsAsync(CancellationToken ct)
    {
        if (_cells.Count == 0) return;

        var random = new Random();
        var rootCell = _cells.First();
        var targetCell = _cells[random.Next(_cells.Count)];

        var eventTypes = new[] { "Heartbeat", "ProcessData", "Heartbeat" };
        var eventType = eventTypes[random.Next(eventTypes.Length)];

        object eventData;
        if (eventType == "ProcessData")
        {
            eventData = new { Value = random.Next(100), Priority = random.Next(1, 5) };
        }
        else
        {
            eventData = new { Interval = random.Next(1, 10) };
        }

        var @event = new FractalEvent(
            $"{eventType.ToLower()}-{DateTime.UtcNow.Ticks}",
            DateTime.UtcNow,
            rootCell.CellId,
            targetCell.CellId,
            eventType,
            new
            {
                Timestamp = DateTime.UtcNow,
                Source = "Orchestrator",
                ActiveCells = _hub.GetActiveCells().Count,
                Data = eventData
            }
        );

        _logger.LogInformation("📤 [ORCHESTRATOR] Sending {EventType} from {Source} to {Target}",
            eventType, rootCell.CellId, targetCell.CellId);

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
