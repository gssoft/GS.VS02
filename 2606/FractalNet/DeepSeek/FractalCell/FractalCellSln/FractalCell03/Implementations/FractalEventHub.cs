// Implementations/FractalEventHub.cs

using System.Collections.Concurrent;
using System.Threading.Channels;

public class InMemoryFractalEventHub : IFractalEventHub
{
    private readonly ConcurrentDictionary<string, CellConnection> _connections = new();
    private readonly ILogger<InMemoryFractalEventHub> _logger;
    private readonly HubSettings _settings;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private record CellConnection(
        Channel<IApplicationEvent>? Channel,
        Func<IApplicationEvent, Task>? Consumer);

    public InMemoryFractalEventHub(
        ILogger<InMemoryFractalEventHub> logger,
        HubSettings? settings = null)
    {
        _logger = logger;
        _settings = settings ?? new HubSettings();
    }

    public Task RegisterChannelAsync(string cellId, Channel<IApplicationEvent> incomingChannel)
    {
        return RegisterInternalAsync(cellId, new CellConnection(incomingChannel, null));
    }

    public Task RegisterConsumerAsync(string cellId, Func<IApplicationEvent, Task> consumer)
    {
        return RegisterInternalAsync(cellId, new CellConnection(null, consumer));
    }

    private async Task RegisterInternalAsync(string cellId, CellConnection connection)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_connections.TryAdd(cellId, connection))
            {
                _logger.LogInformation("Cell {CellId} registered", cellId);
            }
            else
            {
                _logger.LogWarning("Cell {CellId} already registered, updating", cellId);
                _connections[cellId] = connection;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task UnregisterCellAsync(string cellId)
    {
        if (_connections.TryRemove(cellId, out _))
        {
            _logger.LogInformation("Cell {CellId} unregistered", cellId);
        }
        return Task.CompletedTask;
    }

    public async Task PublishAsync(string targetCellId, IApplicationEvent @event)
    {
        if (_connections.TryGetValue(targetCellId, out var connection))
        {
            try
            {
                if (connection.Channel != null)
                {
                    await connection.Channel.Writer.WriteAsync(@event);
                }
                else if (connection.Consumer != null)
                {
                    await connection.Consumer(@event);
                }
                else
                {
                    _logger.LogWarning("Cell {TargetCell} has no active connection", targetCellId);
                }

                _logger.LogDebug("Event {EventId} sent to {TargetCell}",
                    @event.EventId, targetCellId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event to {TargetCell}", targetCellId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Target cell {TargetCell} not found", targetCellId);
            throw new KeyNotFoundException($"Cell {targetCellId} not found");
        }
    }

    public async Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        var tasks = _connections
            .Where(kvp => filter == null || filter(kvp.Key))
            .Select(async kvp =>
            {
                try
                {
                    if (kvp.Value.Channel != null)
                    {
                        await kvp.Value.Channel.Writer.WriteAsync(@event);
                    }
                    else if (kvp.Value.Consumer != null)
                    {
                        await kvp.Value.Consumer(@event);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast to {Cell}", kvp.Key);
                }
            });

        await Task.WhenAll(tasks);
        _logger.LogDebug("Event {EventId} broadcast to {Count} cells",
            @event.EventId, tasks.Count());
    }

    public IReadOnlyCollection<string> GetActiveCells()
    {
        return _connections.Keys.ToList().AsReadOnly();
    }
}
