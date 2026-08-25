using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MicroPlatform.Abstractions;

namespace MicroPlatform.Core;

public class InMemoryMicroEventBus : IMicroEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly ILogger<InMemoryMicroEventBus> _logger;
    private readonly ActivitySource _activitySource = new("MicroEventBus");

    public InMemoryMicroEventBus(ILogger<InMemoryMicroEventBus>? logger = null)
    {
        _logger = logger ?? NullLogger<InMemoryMicroEventBus>.Instance;
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var type = typeof(TEvent);
        _handlers.AddOrUpdate(type, _ => new List<Delegate> { handler }, (_, list) =>
        {
            list.Add(handler);
            return list;
        });
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
            list.Remove(handler);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity($"Publish {typeof(TEvent).Name}", ActivityKind.Producer);
        var type = typeof(TEvent);

        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var del in handlers.Cast<Func<TEvent, CancellationToken, Task>>())
            {
                await del(@event, cancellationToken);
            }
        }
        _logger.LogDebug("Published {EventType}", type.Name);
    }

    public void Dispose() { /* Для InMemory ничего не требуется */ }
}
