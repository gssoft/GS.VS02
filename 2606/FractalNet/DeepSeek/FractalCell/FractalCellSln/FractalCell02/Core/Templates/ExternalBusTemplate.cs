// Core/Templates/ExternalBusTemplate.cs

// Шаблон внешней шины
public abstract class ExternalBusTemplate : IExternalBus
{
    protected readonly BusConfiguration Config;
    public string BusId { get; }
    protected IFractalEventHub? _connectedHub;

    protected ExternalBusTemplate(string busId, BusConfiguration config)
    {
        BusId = busId;
        Config = config;
    }

    public abstract Task ConnectToHubAsync(IFractalEventHub hub, string cellId);
    public abstract Task SendToCellAsync(string targetCellId, IApplicationEvent @event);
    public abstract Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null);
    public abstract IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct);
}
