// Implementations/TplDataflow/TplExternalBus.cs

using System.Threading.Tasks.Dataflow;

public class TplExternalBus : ExternalBusTemplate
{
    private readonly BufferBlock<IApplicationEvent> _bufferBlock;
    private IFractalEventHub? _hub;
    private string? _cellId;

    public TplExternalBus(string busId, BusConfiguration config)
        : base(busId, config)
    {
        _bufferBlock = new BufferBlock<IApplicationEvent>(
            new DataflowBlockOptions { BoundedCapacity = config.Capacity });
    }

    public override async Task ConnectToHubAsync(IFractalEventHub hub, string cellId)
    {
        _hub = hub;
        _cellId = cellId;
        // Регистрируем callback для приема событий
        await hub.RegisterCellAsync(cellId, null); // Нужен адаптер
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

    public override async IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var @event = await _bufferBlock.ReceiveAsync(ct);
            yield return @event;
        }
    }
}
