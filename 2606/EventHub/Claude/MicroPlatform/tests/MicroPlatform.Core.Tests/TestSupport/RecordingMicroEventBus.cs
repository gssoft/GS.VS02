using MicroPlatform.Core;

namespace MicroPlatform.Core.Tests.TestSupport;

/// <summary>
/// Wraps a real IMicroEventBus and records every published event, while still
/// forwarding Subscribe/Unsubscribe/PublishAsync to the inner bus so handlers
/// actually run. This is a *decorator*, not a replacement - a bus that recorded
/// events without dispatching them (as in an earlier draft of this project)
/// would make every downstream processor unreachable.
/// </summary>
public sealed class RecordingMicroEventBus : IMicroEventBus
{
    private readonly IMicroEventBus _inner;
    private readonly List<object> _published = new();
    private readonly object _lock = new();

    public RecordingMicroEventBus(IMicroEventBus inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<object> PublishedEvents
    {
        get
        {
            lock (_lock)
            {
                return _published.ToList();
            }
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        => _inner.Subscribe(handler);

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        => _inner.Unsubscribe(handler);

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _published.Add(@event!);
        }

        await _inner.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
    }
}
