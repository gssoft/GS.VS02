// 26.08.29
// InMemoryMicroEventBus.cs
//
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

    //public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    //{
    //    var eventType = typeof(TEvent);
    //    var wrappedHandler = new Func<object, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

    //    while (true)
    //    {
    //        var currentList = _handlers.TryGetValue(eventType, out var list)
    //            ? list
    //            : ImmutableList<Func<object, CancellationToken, Task>>.Empty;
    //        var newList = currentList.Add(wrappedHandler);
    //        if (_handlers.TryUpdate(eventType, newList, currentList))
    //            break;
    //    }
    //}

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var eventType = typeof(TEvent);
        // Для простоты удаление не поддерживается (в текущем коде не используется)
        // Можно реализовать через поиск по ссылке на wrapped делегат, если потребуется.
        throw new NotSupportedException("Unsubscribe is not supported in this implementation.");
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            // Запускаем все обработчики параллельно
            var tasks = handlers.Select(h => h(@event, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}

// 26.08.26
//// InMemoryMicroEventBus.cs
//using System.Collections.Concurrent;

//namespace Trading.Core;

//public class InMemoryMicroEventBus : IMicroEventBus
//{
//    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = new();

//    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
//    {
//        var eventType = typeof(TEvent);
//        _handlers.AddOrUpdate(eventType,
//            _ => new List<Func<object, CancellationToken, Task>> { (e, ct) => handler((TEvent)e, ct) },
//            (_, list) =>
//            {
//                list.Add((e, ct) => handler((TEvent)e, ct));
//                return list;
//            });
//    }

//    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
//    {
//        if (_handlers.TryGetValue(typeof(TEvent), out var list))
//            list.Remove((e, ct) => handler((TEvent)e, ct));
//    }

//    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
//    {
//        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
//        {
//            foreach (var handler in handlers)
//                await handler(@event, cancellationToken);
//        }
//    }
//}