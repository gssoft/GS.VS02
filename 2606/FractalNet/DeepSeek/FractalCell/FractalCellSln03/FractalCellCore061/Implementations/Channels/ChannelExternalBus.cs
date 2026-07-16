// Implementations/Channels/ChannelExternalBus.cs

using System.Runtime.CompilerServices;

using System.Threading.Channels;
using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Implementations.Channels;

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

        _logger?.LogInformation("ChannelExternalBus {BusId} created with capacity {Capacity}",
            busId, config.Capacity);
    }

    public override async Task ConnectToHubAsync(IFractalEventHub hub, string cellId)
    {
        _hub = hub;
        _cellId = cellId;
        await hub.RegisterChannelAsync(cellId, _incomingChannel);
        _logger?.LogInformation("ChannelExternalBus {BusId} connected to hub for cell {CellId}",
            BusId, cellId);
    }

    public override async Task SendToCellAsync(string targetCellId, IApplicationEvent @event)
    {
        if (_hub == null)
            throw new InvalidOperationException("Hub not connected");

        _logger?.LogInformation("ChannelExternalBus {BusId} sending event {EventId} to {TargetCell}",
            BusId, @event.EventId, targetCellId);

        await _hub.PublishAsync(targetCellId, @event);
    }

    public override async Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        if (_hub == null)
            throw new InvalidOperationException("Hub not connected");

        _logger?.LogInformation("ChannelExternalBus {BusId} broadcasting event {EventId}",
            BusId, @event.EventId);

        await _hub.PublishToAllAsync(@event, filter);
    }

    public override async IAsyncEnumerable<IApplicationEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        _logger?.LogDebug("ChannelExternalBus {BusId} starting to read events", BusId);

        var reader = _incomingChannel.Reader;
        var buffer = Channel.CreateUnbounded<IApplicationEvent>();
        var writer = buffer.Writer;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var @event in reader.ReadAllAsync(ct))
                {
                    await writer.WriteAsync(@event, ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("ChannelExternalBus {BusId} read loop canceled", BusId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ChannelExternalBus {BusId} read loop error", BusId);
            }
            finally
            {
                writer.Complete();
            }
        }, ct);

        await foreach (var @event in buffer.Reader.ReadAllAsync(ct))
        {
            yield return @event;
        }

        _logger?.LogDebug("ChannelExternalBus {BusId} finished reading events", BusId);
    }

    public override void Dispose()
    {
        _logger?.LogInformation("ChannelExternalBus {BusId} disposing", BusId);
        _incomingChannel.Writer.Complete();
        base.Dispose();
    }
}
