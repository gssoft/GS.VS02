// FractalCell.cs
// using EventDataFlow.Core.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

public abstract class FractalCell : BackgroundService
{
    private readonly Channel<IApplicationEvent> _incomingChannel;
    private readonly MicroEventBus _localMicroBus;
    private readonly IFractalEventHub _fractalHub;
    protected readonly ILogger Logger;  // !!!!!!
    // private readonly ILogger _logger;

    public string CellId { get; }
    public CellAddress Address { get; }

    protected FractalCell(
    string cellId,
    CellAddress address,
    IFractalEventHub fractalHub,
    ILogger logger)
    {
        CellId = cellId;
        Address = address;
        _fractalHub = fractalHub;
        Logger = logger;  // ← ВОТ ТАК ПРАВИЛЬНО

        _incomingChannel = Channel.CreateBounded<IApplicationEvent>(1000);
        _localMicroBus = new MicroEventBus();

        ConfigureLocalHandlers();
    }

    //protected FractalCell(
    //    string cellId,
    //    CellAddress address,
    //    IFractalEventHub fractalHub,
    //    ILogger logger)
    //{
    //    CellId = cellId;
    //    Address = address;
    //    _fractalHub = fractalHub;
    //    _logger = Logger;    // !!!!!

    //    _incomingChannel = Channel.CreateBounded<IApplicationEvent>(1000);
    //    _localMicroBus = new MicroEventBus();

    //    ConfigureLocalHandlers();
    //}

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _fractalHub.RegisterCellAsync(CellId, _incomingChannel);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _fractalHub.UnregisterCellAsync(CellId);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("FractalCell {CellId} at {Address} is starting",  // !!!!
            CellId, Address.FullPath);

        await foreach (var @event in _incomingChannel.Reader.ReadAllAsync(stoppingToken))  // !!!!
        {
            try
            {
                await ProcessEventAsync(@event);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing event in cell {CellId}", CellId); // !!!!!
            }
        }
    }

    protected virtual async Task ProcessEventAsync(IApplicationEvent @event)
    {
        // Обрабатываем через локальную шину
        await _localMicroBus.PublishAsync(@event);

        // Если это FractalEvent - проверяем, нужно ли маршрутизировать дальше
        if (@event is FractalEvent fractalEvent)
        {
            await HandleFractalEventAsync(fractalEvent);
        }
    }

    protected virtual Task HandleFractalEventAsync(FractalEvent fractalEvent)
    {
        // По умолчанию - просто логируем
        Logger.LogInformation(    // !!!!!!
            "FractalEvent from {Source} to {Target} in cell {CellId}: {EventType}",
            fractalEvent.SourceCellId,
            fractalEvent.TargetCellId,
            CellId,
            fractalEvent.EventType);

        return Task.CompletedTask;
    }

    // Метод для отправки события в другую ячейку
    protected async Task SendToCellAsync(string targetCellId, string eventType, object payload)
    {
        var fractalEvent = new FractalEvent(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            CellId,
            targetCellId,
            eventType,
            payload
        );

        await _fractalHub.PublishAsync(targetCellId, fractalEvent);
    }

    // Метод для широковещательной рассылки
    protected async Task BroadcastAsync(string eventType, object payload,
        Predicate<string>? filter = null)
    {
        var fractalEvent = new FractalEvent(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            CellId,
            "*", // broadcast
            eventType,
            payload
        );

        await _fractalHub.PublishToAllAsync(fractalEvent, filter);
    }

    // Подписка на локальные обработчики
    protected abstract void ConfigureLocalHandlers();

    // Защищенный доступ к локальной шине для наследников
    protected void SubscribeLocal<T>(Func<T, Task> handler) where T : IApplicationEvent
    {
        _localMicroBus.Subscribe(handler);
    }
}
