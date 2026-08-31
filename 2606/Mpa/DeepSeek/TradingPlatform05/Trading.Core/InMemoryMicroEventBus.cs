using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Trading.Core;

public class InMemoryMicroEventBus : IMicroEventBus
{
    private readonly ConcurrentDictionary<Type, ImmutableList<Func<object, CancellationToken, Task>>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var eventType = typeof(TEvent);
        var wrappedHandler = new Func<object, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

        _handlers.AddOrUpdate(
            eventType,
            _ => ImmutableList<Func<object, CancellationToken, Task>>.Empty.Add(wrappedHandler),
            (_, existingList) => existingList.Add(wrappedHandler));
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        // В текущей реализации не используется; можно реализовать через замену списка.
        throw new NotSupportedException("Unsubscribe is not supported in this implementation.");
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            foreach (var handler in handlers)
                await handler(@event, cancellationToken);
        }
    }
}