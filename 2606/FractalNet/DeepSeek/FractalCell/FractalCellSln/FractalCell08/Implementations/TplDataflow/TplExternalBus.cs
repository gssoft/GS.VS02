using FractalCell.Core.Configuration;
using FractalCell.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using FractalCell.Core.Templates;

namespace FractalCell.Implementations.TplDataflow;

public class TplExternalBus : ExternalBusTemplate
{
    private readonly BufferBlock<IApplicationEvent> _bufferBlock;
    private readonly Channel<IApplicationEvent> _incomingChannel;
    private IFractalEventHub? _hub;
    private string? _cellId;
    private CancellationTokenSource? _cts;

    public TplExternalBus(string busId, BusSettings config, ILogger? logger = null)
        : base(busId, config, logger)
    {
        _bufferBlock = new BufferBlock<IApplicationEvent>(
            new DataflowBlockOptions
            {
                BoundedCapacity = config.Capacity,
                EnsureOrdered = true
            });

        _incomingChannel = Channel.CreateBounded<IApplicationEvent>(
            new BoundedChannelOptions(config.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public override async Task ConnectToHubAsync(IFractalEventHub hub, string cellId)
    {
        _hub = hub;
        _cellId = cellId;
        _cts = new CancellationTokenSource();

        await hub.RegisterConsumerAsync(cellId, async @event =>
        {
            await _bufferBlock.SendAsync(@event, _cts.Token);
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var @event in _incomingChannel.Reader.ReadAllAsync(_cts.Token))
                {
                    await _bufferBlock.SendAsync(@event, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in TplExternalBus forwarding task");
            }
        }, _cts.Token);
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

    public override async IAsyncEnumerable<IApplicationEvent> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger?.LogDebug("TplExternalBus {BusId} starting to read events", BusId);

        var buffer = Channel.CreateUnbounded<IApplicationEvent>();
        var writer = buffer.Writer;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var @event = await _bufferBlock.ReceiveAsync(ct);
                    await writer.WriteAsync(@event, ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("TplExternalBus {BusId} read loop canceled", BusId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TplExternalBus {BusId} read loop error", BusId);
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
    }

    public override void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        base.Dispose();
    }
}
