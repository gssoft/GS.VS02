using System.Collections.Concurrent;
using System.Threading.Channels;

using FractalCell.Core.Configuration;
using FractalCell.Core.Interfaces;

using Microsoft.Extensions.Logging;

namespace FractalCell.Implementations;

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
        _logger.LogInformation("InMemoryFractalEventHub created");
    }

    public Task RegisterChannelAsync(string cellId, Channel<IApplicationEvent> incomingChannel)
    {
        _logger.LogInformation("Registering channel for cell {CellId}", cellId);
        return RegisterInternalAsync(cellId, new CellConnection(incomingChannel, null));
    }

    public Task RegisterConsumerAsync(string cellId, Func<IApplicationEvent, Task> consumer)
    {
        _logger.LogInformation("Registering consumer for cell {CellId}", cellId);
        return RegisterInternalAsync(cellId, new CellConnection(null, consumer));
    }

    private async Task RegisterInternalAsync(string cellId, CellConnection connection)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_connections.TryAdd(cellId, connection))
            {
                _logger.LogInformation("Cell {CellId} registered successfully. Total cells: {Count}",
                    cellId, _connections.Count);
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
        _logger.LogInformation("Publishing event {EventId} to target cell {TargetCell}. Active cells: {Count}",
            @event.EventId, targetCellId, _connections.Count);

        if (_connections.TryGetValue(targetCellId, out var connection))
        {
            try
            {
                if (connection.Channel != null)
                {
                    _logger.LogDebug("Writing event {EventId} to channel of {TargetCell}",
                        @event.EventId, targetCellId);
                    await connection.Channel.Writer.WriteAsync(@event);
                    _logger.LogInformation("Event {EventId} successfully sent to {TargetCell}",
                        @event.EventId, targetCellId);
                }
                else if (connection.Consumer != null)
                {
                    _logger.LogDebug("Invoking consumer for event {EventId} in {TargetCell}",
                        @event.EventId, targetCellId);
                    await connection.Consumer(@event);
                    _logger.LogInformation("Event {EventId} successfully consumed by {TargetCell}",
                        @event.EventId, targetCellId);
                }
                else
                {
                    _logger.LogWarning("Cell {TargetCell} has no active connection", targetCellId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event {EventId} to {TargetCell}",
                    @event.EventId, targetCellId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Target cell {TargetCell} not found. Available cells: {Cells}",
                targetCellId, string.Join(", ", _connections.Keys));
            throw new KeyNotFoundException($"Cell {targetCellId} not found");
        }
    }

    public async Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null)
    {
        _logger.LogInformation("Broadcasting event {EventId} to all cells", @event.EventId);

        var tasks = _connections
            .Where(kvp => filter == null || filter(kvp.Key))
            .Select(async kvp =>
            {
                try
                {
                    if (kvp.Value.Channel != null)
                    {
                        await kvp.Value.Channel.Writer.WriteAsync(@event);
                        _logger.LogDebug("Event {EventId} broadcasted to {Cell}",
                            @event.EventId, kvp.Key);
                    }
                    else if (kvp.Value.Consumer != null)
                    {
                        await kvp.Value.Consumer(@event);
                        _logger.LogDebug("Event {EventId} consumed by {Cell}",
                            @event.EventId, kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast to {Cell}", kvp.Key);
                }
            });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Event {EventId} broadcasted to {Count} cells",
            @event.EventId, tasks.Count());
    }

    public IReadOnlyCollection<string> GetActiveCells()
    {
        var cells = _connections.Keys.ToList().AsReadOnly();
        _logger.LogDebug("Getting active cells: {Count} cells", cells.Count);
        return cells;
    }
}
