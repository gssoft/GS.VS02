// Worker.cs
using System.Runtime.InteropServices.JavaScript;

using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFractalEventHub _hub;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<FractalCellTemplate<IInternalBus, IExternalBus>> _cells = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Worker(
        ILogger<Worker> logger,
        IFractalEventHub hub,
        IHostApplicationLifetime lifetime,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _hub = hub;
        _lifetime = lifetime;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Создаем фрактальную иерархию ячеек
            await CreateFractalHierarchyAsync(stoppingToken);

            _logger.LogInformation("All cells started. Active cells: {Count}",
                _hub.GetActiveCells().Count);

            // Запускаем тестовые сценарии
            await RunTestScenariosAsync(stoppingToken);

            // Ждем завершения
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker error");
            _lifetime.StopApplication();
        }
    }

    private async Task CreateFractalHierarchyAsync(CancellationToken ct)
    {
        // Создаем корневую ячейку
        var rootCell = await CreateCellAsync("Root", 3, ct);
        _cells.Add(rootCell);

        // Создаем дочерние ячейки
        var childCells = new[] { "Child-A", "Child-B", "Child-C" };
        foreach (var childId in childCells)
        {
            var child = await CreateCellAsync(childId, 2, ct);
            _cells.Add(child);

            // Создаем связь между ячейками через конфигурацию
            // В реальном приложении здесь может быть динамическая маршрутизация
        }

        // Запускаем все ячейки
        await Task.WhenAll(_cells.Select(c => c.StartAsync(ct)));
    }

    private async Task<FractalCellTemplate<IInternalBus, IExternalBus>> CreateCellAsync(
        string cellId,
        int workers,
        CancellationToken ct)
    {
        var config = new FractalCellConfiguration
        {
            CellId = cellId,
            BackgroundServiceCount = workers,
            InternalBusType = BusType.Channels,
            ExternalBusType = BusType.TplDataflow,
            BusSettings = new BusSettings
            {
                Capacity = 1000,
                MaxParallelism = 4
            }
        };

        // Используем фабрику
        return await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);
    }

    private async Task RunTestScenariosAsync(CancellationToken ct)
    {
        if (_cells.Count == 0) return;

        var random = new Random();
        var rootCell = _cells.First();
        int eventCount = 0;

        // Отправляем события каждые 2 секунды
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(2000, ct);

            var targetCell = _cells[random.Next(_cells.Count)].Configuration.CellId;
            var @event = new FractalEvent(
                $"evt-{eventCount++}",
                DateTime.UtcNow,
                rootCell.Configuration.CellId,
                targetCell,
                eventCount % 2 == 0 ? "ProcessData" : "Heartbeat",
                new { Data = $"Payload-{eventCount}", Timestamp = DateTime.UtcNow }
            );

            _logger.LogInformation("Sending event {EventId} to {TargetCell}",
                @event.EventId, targetCell);

            await rootCell.ExternalBus.SendToCellAsync(targetCell, @event);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all cells...");

        await Task.WhenAll(_cells.Select(c => c.StopAsync(cancellationToken)));
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _cells.Clear();
        }
        finally
        {
            _semaphore.Release();
        }

        await base.StopAsync(cancellationToken);
    }
}