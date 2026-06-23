// MicroEventBus.cs

// using EventDataFlow.Core.Interfaces;
using System.Collections.Concurrent;

public class MicroEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();
    private readonly ConcurrentDictionary<string, object> _state = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent
    {
        var eventType = typeof(TEvent);
        var wrappedHandler = new Func<IApplicationEvent, Task>(e =>
        {
            if (e is TEvent typedEvent)
                return handler(typedEvent);
            return Task.CompletedTask;
        });

        _handlers.AddOrUpdate(
            eventType,
            _ => new List<Func<IApplicationEvent, Task>> { wrappedHandler },
            (_, list) => { list.Add(wrappedHandler); return list; }
        );
    }

    public async Task PublishAsync(IApplicationEvent @event)
    {
        if (_handlers.TryGetValue(@event.GetType(), out var handlers))
        {
            var tasks = handlers.Select(h => SafeExecuteAsync(h, @event));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SafeExecuteAsync(Func<IApplicationEvent, Task> handler, IApplicationEvent @event)
    {
        try
        {
            await handler(@event);
        }
        catch (Exception ex)
        {
            // Здесь можно добавить Dead Letter Queue
            Console.WriteLine($"Error in handler for event {@event.GetType().Name}: {ex.Message}");
        }
    }

    public TState GetOrAddState<TState>(string key, Func<TState> factory) where TState : class
    {
        return (TState)_state.GetOrAdd(key, _ => factory());
    }

    public void SetState<TState>(string key, TState value) where TState : class
    {
        _state[key] = value;
    }
}
