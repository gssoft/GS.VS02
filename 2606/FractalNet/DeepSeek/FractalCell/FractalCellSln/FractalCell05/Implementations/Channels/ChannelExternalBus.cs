using System.Threading.Channels;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Implementations.Channels;

public class ChannelExternalBus : ExternalBusTemplate
{
    private readonly Channel<IApplicationEvent> _incomingChannel;
    private IFractalEventHub? _hub;
    private string? _cellId;

    public ChannelExternalBus(string busId, BusSettings config, ILogger? logger = null)
        : base(busId, config, logger)
    {
        _incomingChannel = Channel.CreateBounded<IApplicationEvent>(
            new BoundedChannelOptions(config.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public override async Task ConnectToHubAsync(IFractalEventHub hub, string cellId)
    {
        _hub = hub;
        _cellId = cellId;
        await hub.RegisterChannelAsync(cellId, _incomingChannel);
    }

    public override async Task SendToCellAsync(string targetCellId, IApplicationEvent @event)
    {
        if (_hub == null)
            throw new InvalidOperationException("Hub not connected");

        await _hub.PublishAsync(targetCellId, @event);
    }

    public override async Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        if (_hub == null)
            throw new InvalidOperationException("Hub not connected");

        await _hub.PublishToAllAsync(@event, filter);
    }

    public override IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct)
    {
        return _incomingChannel.Reader.ReadAllAsync(ct);
    }

    public override void Dispose()
    {
        _incomingChannel.Writer.Complete();
        base.Dispose();
    }
}
