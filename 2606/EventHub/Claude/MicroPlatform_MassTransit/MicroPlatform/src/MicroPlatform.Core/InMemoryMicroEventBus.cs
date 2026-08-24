using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MicroPlatform.Core;

/// <summary>
/// In-process, in-memory implementation of <see cref="IMicroEventBus"/>.
///
/// Design decisions (each one fixes a bug found earlier in this same conversation):
/// - Handler lists are ImmutableList, not List{T}, so Publish always iterates over
///   a stable snapshot even if Subscribe/Unsubscribe happens concurrently.
/// - A handler that throws does not stop the remaining handlers from running;
///   all exceptions are collected and re-thrown together as an AggregateException
///   once every handler has had a chance to run.
/// - Subscribe/Unsubscribe rely on delegate equality. This only works reliably when
///   the exact same delegate instance is passed to both calls (e.g. a method group
///   captured once in a field) - see OrderProcessor/InventoryProcessor for the pattern.
/// </summary>
public sealed class InMemoryMicroEventBus : IMicroEventBus
{
    private readonly ConcurrentDictionary<Type, ImmutableList<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.AddOrUpdate(
            typeof(TEvent),
            addValueFactory: _ => ImmutableList.Create<Delegate>(handler),
            updateValueFactory: (_, existing) => existing.Add(handler));
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.AddOrUpdate(
            typeof(TEvent),
            addValueFactory: _ => ImmutableList<Delegate>.Empty,
            updateValueFactory: (_, existing) => existing.Remove(handler));
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers) || handlers.IsEmpty)
            return;

        List<Exception>? exceptions = null;

        // handlers is an immutable snapshot: safe to iterate even if another thread
        // subscribes/unsubscribes for the same event type right now.
        foreach (var handlerDelegate in handlers)
        {
            var handler = (Func<TEvent, CancellationToken, Task>)handlerDelegate;
            try
            {
                await handler(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more handlers for {typeof(TEvent).Name} threw an exception.",
                exceptions);
        }
    }
}
