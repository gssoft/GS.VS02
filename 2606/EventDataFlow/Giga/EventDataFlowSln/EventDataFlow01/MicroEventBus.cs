// MicroEventBus.cs
using EventDataFlow01;
using System.Collections.Concurrent;

public class MicroEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent
    {
        var eventType = typeof(TEvent);
        var handlerToAdd = new Func<IApplicationEvent, Task>(e => handler((TEvent)e));

        _handlers.AddOrUpdate(eventType,
            _ => [handlerToAdd],
            (_, list) => { list.Add(handlerToAdd); return list; });
    }

    public async Task PublishAsync(IApplicationEvent @event)
    {
        if (_handlers.TryGetValue(@event.GetType(), out var handlers))
        {
            // Вызываем все обработчики для данного типа события
            // Используем WhenAll для параллельного выполнения, но не ждем его в основном цикле
            await Task.WhenAll(handlers.Select(h => h(@event)));
        }
    }
}
