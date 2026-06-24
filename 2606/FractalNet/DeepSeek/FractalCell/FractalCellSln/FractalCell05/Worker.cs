using FractalCell02.Core;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
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
        _logger.LogInformation("🚀 Worker (Orchestrator) started");

        try
        {
            // 1. Инициализация системы
            await InitializeSystemAsync(stoppingToken);

            _logger.LogInformation("✅ System initialized. Starting orchestration loop...");

            // 2. ОСНОВНОЙ БЕСКОНЕЧНЫЙ ЦИКЛ ОРКЕСТРАТОРА
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Отправляем heartbeat
                    await OrchestrateAsync(stoppingToken);

                    // Ждем перед следующей итерацией
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

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

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
//                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//                }
//                catch (OperationCanceledException)
//                {
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in orchestration loop");
//                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

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
//            // 1. Создаем и запускаем ячейки
//            await InitializeSystemAsync(stoppingToken);

//            _logger.LogInformation("✅ System initialized. Starting orchestration loop...");

//            // 2. ОРКЕСТРАТОР - бесконечный цикл
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await OrchestrateAsync(stoppingToken);
//                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//                }
//                catch (OperationCanceledException)
//                {
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in orchestration loop");
//                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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

//        // Запускаем все ячейки - StartAsync теперь НЕ БЛОКИРУЕТ!
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

//// Worker orkestartor

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private bool _isInitialized;

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
//            // Инициализация системы
//            await InitializeSystemAsync(stoppingToken);
//            _isInitialized = true;

//            _logger.LogInformation("✅ System initialized. Starting orchestration loop...");

//            // ГЛАВНЫЙ БЕСКОНЕЧНЫЙ ЦИКЛ ОРКЕСТРАТОРА
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    // Выполняем задачи оркестрации
//                    await OrchestrateAsync(stoppingToken);

//                    // Пауза перед следующим циклом
//                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Orchestration loop canceled");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in orchestration loop");
//                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
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

//        // Запускаем все ячейки
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
//        if (!_isInitialized || _cells.Count == 0)
//            return;

//        try
//        {
//            // 1. Проверка здоровья системы
//            await CheckSystemHealthAsync(ct);

//            // 2. Отправка тестовых событий
//            await SendTestEventsAsync(ct);

//            // 3. Сбор статистики
//            await CollectStatisticsAsync(ct);

//            // 4. Балансировка нагрузки (если нужно)
//            await BalanceLoadAsync(ct);

//            _logger.LogDebug("✅ Orchestration cycle completed");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error during orchestration");
//        }
//    }

//    private async Task CheckSystemHealthAsync(CancellationToken ct)
//    {
//        var activeCells = _hub.GetActiveCells();
//        _logger.LogDebug("🏥 Health check: {Active}/{Total} cells active",
//            activeCells.Count, _cells.Count);
//        await Task.CompletedTask;
//    }

//    private async Task SendTestEventsAsync(CancellationToken ct)
//    {
//        if (_cells.Count < 2) return;

//        var random = new Random();
//        var rootCell = _cells.First();
//        var targetCell = _cells[random.Next(1, _cells.Count)]; // Не отправляем себе

//        var @event = new FractalEvent(
//            $"evt-{DateTime.UtcNow.Ticks}",
//            DateTime.UtcNow,
//            rootCell.CellId,
//            targetCell.CellId,
//            "Heartbeat",
//            new
//            {
//                Timestamp = DateTime.UtcNow,
//                Source = "Orchestrator"
//            }
//        );

//        _logger.LogInformation("📤 [ORCHESTRATOR] Sending heartbeat from {Source} to {Target}",
//            rootCell.CellId, targetCell.CellId);

//        await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);
//    }

//    private async Task CollectStatisticsAsync(CancellationToken ct)
//    {
//        // Здесь можно собирать статистику о работе ячеек
//        _logger.LogDebug("📊 Collecting system statistics...");
//        await Task.CompletedTask;
//    }

//    private async Task BalanceLoadAsync(CancellationToken ct)
//    {
//        // Здесь можно балансировать нагрузку между ячейками
//        _logger.LogDebug("⚖️ Checking load balance...");
//        await Task.CompletedTask;
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly ManualResetEventSlim _stopEvent = new(false);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Ждем сигнала остановки - НИКОГДА НЕ ВЫХОДИМ САМИ
//            _stopEvent.WaitHandle.WaitOne();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            await Task.Delay(2000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        // Сигнализируем о остановке
//        _stopEvent.Set();

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. ЗАПУСКАЕМ ТЕСТЫ СРАЗУ ПОСЛЕ ЗАПУСКА ЯЧЕЕК
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Ждем завершения тестов или отмены
//            await _testTask;
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Небольшая задержка для стабилизации
//            await Task.Delay(2000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

// Working well, but we are not see it.

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // Запускаем тестовые сценарии в фоне
//            _ = Task.Run(async () => await RunTestScenariosAsync(stoppingToken), stoppingToken);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // БЕСКОНЕЧНЫЙ ЦИКЛ - НИКОГДА НЕ ВЫХОДИМ
//            while (true)
//            {
//                await Task.Delay(1000);
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly ManualResetEventSlim _stopEvent = new(false);
//    private Task? _testTask;
//    private CancellationTokenSource? _testCts;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Создаем отдельный токен для тестов
//            _testCts = new CancellationTokenSource();

//            // 3. Запускаем тестовые сценарии
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Enter to stop.");

//            // 4. Ждем сигнала остановки через ManualResetEvent
//            await Task.Run(() => _stopEvent.WaitHandle.WaitOne());
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        // Сигнализируем о остановке
//        _stopEvent.Set();

//        // Отменяем тесты
//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        // Ждем завершения тестов
//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}


//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private Task? _testTask;
//    private CancellationTokenSource? _testCts;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Создаем отдельный токен для тестов, связанный с основным
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

//            // 3. Запускаем тестовые сценарии и ждем их завершения или отмены
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C or Enter to stop.");

//            // 4. Ждем либо отмены, либо завершения тестов
//            await Task.WhenAny(
//                Task.Delay(-1, stoppingToken),
//                _testTask
//            );
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        // Отменяем тесты
//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        // Ждем завершения тестов
//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}


//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _ = Task.Run(async () => await RunTestScenariosAsync(stoppingToken), stoppingToken);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Бесконечный цикл - ждем пока приложение не остановят
//            while (true)
//            {
//                await Task.Delay(1000);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

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
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _ = Task.Run(async () => await RunTestScenariosAsync(stoppingToken), stoppingToken);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Ждем отмены
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;
//    private readonly ManualResetEventSlim _stopEvent = new(false);

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Ждем сигнала остановки через ManualResetEvent
//            await Task.Run(() => _stopEvent.WaitHandle.WaitOne());

//            _logger.LogInformation("👋 Stop signal received");
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(2000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        // Сигнализируем о остановке
//        _stopEvent.Set();

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне (сразу после запуска ячеек)
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // 3. Ждем отмены или завершения тестов
//            while (!stoppingToken.IsCancellationRequested && !_testTask.IsCompleted)
//            {
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//            _lifetime.StopApplication();
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(2000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask;
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // Запускаем тестовые сценарии
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ System is running. Press Ctrl+C to stop.");

//            // Ждем отмены или завершения тестов
//            while (!stoppingToken.IsCancellationRequested && !_testTask.IsCompleted)
//            {
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(2000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask;
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ Waiting for events... Press Ctrl+C to stop");

//            // 3. Ждем сигнала остановки
//            await Task.Delay(Timeout.Infinite, stoppingToken);

//            _logger.LogInformation("👋 Worker stopping due to cancellation");
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//            _lifetime.StopApplication();
//        }
//        finally
//        {
//            _logger.LogInformation("Worker ExecuteAsync finished");
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        try
//        {
//            // Ждем, пока все ячейки полностью запустятся
//            await Task.Delay(3000, ct);

//            if (_cells.Count == 0)
//            {
//                _logger.LogWarning("⚠️ No cells to run test scenarios");
//                return;
//            }

//            var random = new Random();
//            var rootCell = _cells.First();
//            int eventCount = 0;

//            _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//                rootCell.CellId, _cells.Count);

//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    // Выбираем случайную целевую ячейку
//                    var targetCell = _cells[random.Next(_cells.Count)];
//                    var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                    var @event = new FractalEvent(
//                        $"evt-{eventCount++:D4}",
//                        DateTime.UtcNow,
//                        rootCell.CellId,
//                        targetCell.CellId,
//                        eventType,
//                        new
//                        {
//                            Data = $"Payload-{eventCount}",
//                            Timestamp = DateTime.UtcNow,
//                            Sequence = eventCount
//                        }
//                    );

//                    _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                        @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                    await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                    _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                    await Task.Delay(3000, ct);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Error in test scenario");
//                    await Task.Delay(5000, ct);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ RunTestScenariosAsync canceled");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Error in RunTestScenariosAsync");
//        }
//        finally
//        {
//            _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            // 1. Создаем иерархию ячеек
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // 2. Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ Waiting for events...");

//            // 3. Ждем сигнала остановки или завершения тестов
//            await Task.WhenAny(
//                Task.Delay(Timeout.Infinite, stoppingToken),
//                _testTask
//            );
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");

//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//        foreach (var cell in _cells)
//        {
//            _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        // Ждем, пока все ячейки полностью запустятся
//        await Task.Delay(2000, ct);

//        if (_cells.Count == 0)
//        {
//            _logger.LogWarning("⚠️ No cells to run test scenarios");
//            return;
//        }

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        _logger.LogInformation("📨 Starting to send test events from {RootCell} to {Count} cells...",
//            rootCell.CellId, _cells.Count);

//        while (!ct.IsCancellationRequested)
//        {
//            try
//            {
//                // Выбираем случайную целевую ячейку
//                var targetCell = _cells[random.Next(_cells.Count)];
//                var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                var @event = new FractalEvent(
//                    $"evt-{eventCount++:D4}",
//                    DateTime.UtcNow,
//                    rootCell.CellId,
//                    targetCell.CellId,
//                    eventType,
//                    new
//                    {
//                        Data = $"Payload-{eventCount}",
//                        Timestamp = DateTime.UtcNow,
//                        Sequence = eventCount
//                    }
//                );

//                _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                    @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                await Task.Delay(3000, ct);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error in test scenario");
//                await Task.Delay(5000, ct);
//            }
//        }

//        _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;
//    private Task? _testTask;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            _logger.LogInformation("🚀 Worker ExecuteAsync started");

//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("🔍 Checking cells: {Count}", _cells.Count);
//            foreach (var cell in _cells)
//            {
//                _logger.LogInformation("🔍 Cell: {CellId}", cell.CellId);
//            }

//            _logger.LogInformation("✅ All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _testTask = RunTestScenariosAsync(_testCts.Token);

//            _logger.LogInformation("⏳ Waiting for events...");

//            // Ждем сигнала остановки
//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🏗️ Creating fractal hierarchy...");



//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        // Запускаем все ячейки
//        _logger.LogInformation("▶️ Starting all cells...");
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("✅ Fractal hierarchy created with {Count} cells", _cells.Count);
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

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        _logger.LogInformation("🔄 RunTestScenariosAsync started");

//        if (_cells.Count == 0)
//        {
//            _logger.LogWarning("⚠️ No cells to run test scenarios");
//            return;
//        }

//        // Небольшая задержка перед началом отправки
//        await Task.Delay(1000, ct);

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        _logger.LogInformation("📨 Starting to send test events from {RootCell}...", rootCell.CellId);

//        while (!ct.IsCancellationRequested)
//        {
//            try
//            {
//                // Выбираем случайную целевую ячейку (включая Root)
//                var targetCell = _cells[random.Next(_cells.Count)];
//                var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                var @event = new FractalEvent(
//                    $"evt-{eventCount++:D4}",
//                    DateTime.UtcNow,
//                    rootCell.CellId,
//                    targetCell.CellId,
//                    eventType,
//                    new
//                    {
//                        Data = $"Payload-{eventCount}",
//                        Timestamp = DateTime.UtcNow,
//                        Sequence = eventCount
//                    }
//                );

//                _logger.LogInformation("📤 [TEST] Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                    @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                // Отправляем событие через внешнюю шину корневой ячейки
//                await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                _logger.LogInformation("✅ [TEST] Event {EventId} sent successfully", @event.EventId);

//                // Ждем перед следующей отправкой
//                await Task.Delay(2000, ct);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("⏹️ Test scenarios stopped by cancellation");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error in test scenario");
//                await Task.Delay(5000, ct); // Ждем перед повторной попыткой
//            }
//        }

//        _logger.LogInformation("🏁 RunTestScenariosAsync finished");
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("🛑 Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        if (_testTask != null && !_testTask.IsCompleted)
//        {
//            try
//            {
//                await _testTask.WaitAsync(cancellationToken);
//            }
//            catch (OperationCanceledException)
//            {
//                // Ожидаемое исключение
//            }
//        }

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        _logger.LogInformation("✅ All cells stopped");
//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _ = RunTestScenariosAsync(_testCts.Token);

//            // Ждем сигнала остановки
//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        // Запускаем все ячейки
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("Fractal hierarchy created with {Count} cells", _cells.Count);
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
//        _logger.LogInformation("Cell {CellId} created with {Workers} workers", cellId, workers);
//        return cell;
//    }

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0)
//        {
//            _logger.LogWarning("No cells to run test scenarios");
//            return;
//        }

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        _logger.LogInformation("Starting test scenarios...");

//        while (!ct.IsCancellationRequested)
//        {
//            try
//            {
//                // Отправляем события каждые 2 секунды
//                await Task.Delay(2000, ct);

//                // Выбираем случайную целевую ячейку (включая Root)
//                var targetCell = _cells[random.Next(_cells.Count)];
//                var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                var @event = new FractalEvent(
//                    $"evt-{eventCount++}",
//                    DateTime.UtcNow,
//                    rootCell.CellId,
//                    targetCell.CellId,
//                    eventType,
//                    new
//                    {
//                        Data = $"Payload-{eventCount}",
//                        Timestamp = DateTime.UtcNow,
//                        Sequence = eventCount
//                    }
//                );

//                _logger.LogInformation("📤 Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                    @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                // Отправляем событие через внешнюю шину корневой ячейки
//                await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);

//                _logger.LogInformation("✅ Event {EventId} sent successfully", @event.EventId);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("Test scenarios stopped");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Error in test scenario");
//            }
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private CancellationTokenSource? _testCts;

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            // Запускаем тестовые сценарии в фоне
//            _testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//            _ = RunTestScenariosAsync(_testCts.Token);

//            // Ждем сигнала остановки
//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        // Создаем корневую ячейку
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        // Создаем дочерние ячейки
//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        // Запускаем все ячейки
//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));

//        _logger.LogInformation("Fractal hierarchy created with {Count} cells", _cells.Count);
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
//        _logger.LogInformation("Cell {CellId} created with {Workers} workers", cellId, workers);
//        return cell;
//    }

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0)
//        {
//            _logger.LogWarning("No cells to run test scenarios");
//            return;
//        }

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        _logger.LogInformation("Starting test scenarios...");

//        while (!ct.IsCancellationRequested)
//        {
//            try
//            {
//                await Task.Delay(3000, ct);

//                // Выбираем случайную целевую ячейку
//                var targetCell = _cells[random.Next(_cells.Count)];
//                var eventType = eventCount % 2 == 0 ? "ProcessData" : "Heartbeat";

//                var @event = new FractalEvent(
//                    $"evt-{eventCount++}",
//                    DateTime.UtcNow,
//                    rootCell.CellId,
//                    targetCell.CellId,
//                    eventType,
//                    new
//                    {
//                        Data = $"Payload-{eventCount}",
//                        Timestamp = DateTime.UtcNow,
//                        Sequence = eventCount
//                    }
//                );

//                _logger.LogInformation("Sending event {EventId} ({EventType}) from {Source} to {TargetCell}",
//                    @event.EventId, eventType, rootCell.CellId, targetCell.CellId);

//                // Отправляем событие через внешнюю шину корневой ячейки
//                await rootCell.ExternalBus.SendToCellAsync(targetCell.CellId, @event);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("Test scenarios stopped");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in test scenario");
//            }
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping all cells...");

//        _testCts?.Cancel();
//        _testCts?.Dispose();

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            await RunTestScenariosAsync(stoppingToken);

//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));
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
//            // Используем ОДИНАКОВЫЕ типы шин для избежания проблем с приведением типов
//            InternalBusType = BusType.Channels,      // Можно изменить на TplDataflow
//            ExternalBusType = BusType.Channels,      // Должен совпадать с InternalBusType
//            BusSettings = new BusSettings
//            {
//                Capacity = 1000,
//                MaxParallelism = 4
//            }
//        };

//        return await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
//    }

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0) return;

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        while (!ct.IsCancellationRequested)
//        {
//            await Task.Delay(2000, ct);

//            var targetCell = _cells[random.Next(_cells.Count)].CellId;
//            var @event = new FractalEvent(
//                $"evt-{eventCount++}",
//                DateTime.UtcNow,
//                rootCell.CellId,
//                targetCell,
//                eventCount % 2 == 0 ? "ProcessData" : "Heartbeat",
//                new { Data = $"Payload-{eventCount}", Timestamp = DateTime.UtcNow }
//            );

//            _logger.LogInformation("Sending event {EventId} to {TargetCell}",
//                @event.EventId, targetCell);

//            await rootCell.ExternalBus.SendToCellAsync(targetCell, @event);
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        await base.StopAsync(cancellationToken);
//    }
//}

//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<IFractalCell> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            await RunTestScenariosAsync(stoppingToken);

//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));
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
//            ExternalBusType = BusType.TplDataflow,
//            BusSettings = new BusSettings
//            {
//                Capacity = 1000,
//                MaxParallelism = 4
//            }
//        };

//        return await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
//    }

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0) return;

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        while (!ct.IsCancellationRequested)
//        {
//            await Task.Delay(2000, ct);

//            var targetCell = _cells[random.Next(_cells.Count)].CellId;
//            var @event = new FractalEvent(
//                $"evt-{eventCount++}",
//                DateTime.UtcNow,
//                rootCell.CellId,
//                targetCell,
//                eventCount % 2 == 0 ? "ProcessData" : "Heartbeat",
//                new { Data = $"Payload-{eventCount}", Timestamp = DateTime.UtcNow }
//            );

//            _logger.LogInformation("Sending event {EventId} to {TargetCell}",
//                @event.EventId, targetCell);

//            await rootCell.ExternalBus.SendToCellAsync(targetCell, @event);
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        await base.StopAsync(cancellationToken);
//    }
//}
//using FractalCell02.Core;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IFractalEventHub _hub;
//    private readonly IHostApplicationLifetime _lifetime;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly List<FractalCellTemplate<IInternalBus, IExternalBus>> _cells = new();
//    private readonly SemaphoreSlim _semaphore = new(1, 1);

//    public Worker(
//        ILogger<Worker> logger,
//        IFractalEventHub hub,
//        IHostApplicationLifetime lifetime,
//        ILoggerFactory loggerFactory)
//    {
//        _logger = logger;
//        _hub = hub;
//        _lifetime = lifetime;
//        _loggerFactory = loggerFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {
//            await CreateFractalHierarchyAsync(stoppingToken);

//            _logger.LogInformation("All cells started. Active cells: {Count}",
//                _hub.GetActiveCells().Count);

//            await RunTestScenariosAsync(stoppingToken);

//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("Worker stopping...");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Worker error");
//            _lifetime.StopApplication();
//        }
//    }

//    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
//    {
//        var rootCell = await CreateCellAsync("Root", 3, ct);
//        _cells.Add(rootCell);

//        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
//        foreach (var childId in childCells)
//        {
//            var child = await CreateCellAsync(childId, 2, ct);
//            _cells.Add(child);
//        }

//        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));
//    }

//    private async Task<FractalCellTemplate<IInternalBus, IExternalBus>> CreateCellAsync(
//        string cellId,
//        int workers,
//        CancellationToken ct)
//    {
//        var config = new FractalCellConfiguration
//        {
//            CellId = cellId,
//            BackgroundServiceCount = workers,
//            InternalBusType = BusType.Channels,
//            ExternalBusType = BusType.TplDataflow,
//            BusSettings = new BusSettings
//            {
//                Capacity = 1000,
//                MaxParallelism = 4
//            }
//        };

//        return await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
//    }

//    private async Task RunTestScenariosAsync(CancellationToken ct)
//    {
//        if (_cells.Count == 0) return;

//        var random = new Random();
//        var rootCell = _cells.First();
//        int eventCount = 0;

//        while (!ct.IsCancellationRequested)
//        {
//            await Task.Delay(2000, ct);

//            var targetCell = _cells[random.Next(_cells.Count)].Configuration.CellId;
//            var @event = new FractalEvent(
//                $"evt-{eventCount++}",
//                DateTime.UtcNow,
//                rootCell.Configuration.CellId,
//                targetCell,
//                eventCount % 2 == 0 ? "ProcessData" : "Heartbeat",
//                new { Data = $"Payload-{eventCount}", Timestamp = DateTime.UtcNow }
//            );

//            _logger.LogInformation("Sending event {EventId} to {TargetCell}",
//                @event.EventId, targetCell);

//            await rootCell.ExternalBus.SendToCellAsync(targetCell, @event);
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping all cells...");

//        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
//        await _semaphore.WaitAsync(cancellationToken);

//        try
//        {
//            _cells.Clear();
//        }
//        finally
//        {
//            _semaphore.Release();
//        }

//        await base.StopAsync(cancellationToken);
//    }
//}


//namespace FractalCell05
//{
//    public class Worker(ILogger<Worker> logger) : BackgroundService
//    {
//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                if (logger.IsEnabled(LogLevel.Information))
//                {
//                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//                }
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//    }
//}
