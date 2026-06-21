// UniversalFractalEventHub.cs
using System.Collections.Concurrent;
using System.Threading.Channels;

public class UniversalFractalEventHub : IFractalEventHub
{
    private readonly ConcurrentDictionary<string, CellInfo> _cells = new();
    private readonly ILogger<UniversalFractalEventHub> _logger;
    private readonly IFractalEventHub? _parentHub;
    private readonly string _hubId;

    public UniversalFractalEventHub(
        ILogger<UniversalFractalEventHub> logger,
        string hubId = "root",
        IFractalEventHub? parentHub = null)
    {
        _logger = logger;
        _hubId = hubId;
        _parentHub = parentHub;
    }

    public Task RegisterCellAsync(string cellId, Channel<IApplicationEvent> incomingChannel,
        FractalEventHubConfiguration? config = null)
    {
        _cells[cellId] = new CellInfo
        {
            Channel = incomingChannel,
            Config = config ?? new FractalEventHubConfiguration()
        };

        _logger.LogInformation("Cell {CellId} registered in hub {HubId}", cellId, _hubId);
        return Task.CompletedTask;
    }

    public Task UnregisterCellAsync(string cellId)
    {
        _cells.TryRemove(cellId, out _);
        _logger.LogInformation("Cell {CellId} unregistered from hub {HubId}", cellId, _hubId);
        return Task.CompletedTask;
    }

    public async Task PublishAsync(string targetCellId, IApplicationEvent @event)
    {
        if (_cells.TryGetValue(targetCellId, out var cellInfo))
        {
            // Отправляем в локальную ячейку
            await SendToChannelAsync(cellInfo, @event);
            return;
        }

        // Если ячейка не найдена локально, пробуем через родительский хаб
        if (_parentHub != null)
        {
            await _parentHub.PublishAsync(targetCellId, @event);
            return;
        }

        _logger.LogWarning("Cell {TargetCellId} not found in any hub. Event lost: {EventId}",
            targetCellId, @event.EventId);
    }

    public async Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        var tasks = _cells
            .Where(c => filter == null || filter(c.Key))
            .Select(cell => SendToChannelAsync(cell.Value, @event));

        await Task.WhenAll(tasks);
    }

    private async Task SendToChannelAsync(CellInfo cellInfo, IApplicationEvent @event)
    {
        try
        {
            var channel = cellInfo.Channel;
            if (!channel.Writer.TryWrite(@event))
            {
                var timeout = cellInfo.Config.MessageTimeout;
                using var cts = new CancellationTokenSource(timeout);
                await channel.Writer.WriteAsync(@event, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Timeout sending event {EventId} to cell", @event.EventId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event {EventId}", @event.EventId);
            throw;
        }
    }

    private record CellInfo
    {
        public required Channel<IApplicationEvent> Channel { get; init; }
        public required FractalEventHubConfiguration Config { get; init; }
    }
}
