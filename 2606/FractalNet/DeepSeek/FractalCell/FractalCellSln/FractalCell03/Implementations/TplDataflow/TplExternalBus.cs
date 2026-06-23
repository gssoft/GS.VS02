// Implementations/TplDataflow/TplExternalBus.cs

using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

public class TplExternalBus : ExternalBusTemplate
{
    private readonly ILogger<TplExternalBus> _logger;
    private readonly BufferBlock<IApplicationEvent> _bufferBlock;
    private readonly Channel<IApplicationEvent> _incomingChannel;
    private IFractalEventHub? _hub;
    private string? _cellId;
    private CancellationTokenSource? _cts;

    public TplExternalBus(string busId, BusSettings config, ILogger<TplExternalBus>  logger)
        : base(busId, config)
    {
        _logger = logger;
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

        // Регистрируем Consumer для приема событий
        await hub.RegisterConsumerAsync(cellId, async @event =>
        {
            await _bufferBlock.SendAsync(@event, _cts.Token);
        });

        // Запускаем фоновую задачу для пересылки из канала в BufferBlock
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

    public override async IAsyncEnumerable<IApplicationEvent>  ReadAllAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var @event = await _bufferBlock.ReceiveAsync(ct);
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

