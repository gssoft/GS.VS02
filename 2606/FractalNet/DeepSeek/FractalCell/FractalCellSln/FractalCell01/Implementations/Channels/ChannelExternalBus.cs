using System.Threading.Channels;

public class ChannelExternalBus : ExternalBusTemplate
{
    private readonly Channel<IApplicationEvent> _incomingChannel;
    private IFractalEventHub? _hub;
    private string? _cellId;

    public ChannelExternalBus(string busId, BusConfiguration config)
        : base(busId, config)
    {
        _incomingChannel = Channel.CreateBounded<IApplicationEvent>(config.Capacity);
    }

    public override async Task ConnectToHubAsync(IFractalEventHub hub, string cellId)
    {
        _hub = hub;
        _cellId = cellId;
        await hub.RegisterCellAsync(cellId, _incomingChannel);
    }

    public override async Task SendToCellAsync(string targetCellId, IApplicationEvent @event)
    {
        if (_hub != null)
            await _hub.PublishAsync(targetCellId, @event);
    }

    public override async Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        if (_hub != null)
            await _hub.PublishToAllAsync(@event, filter);
    }

    public override IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct)
    {
        return _incomingChannel.Reader.ReadAllAsync(ct);
    }
}

