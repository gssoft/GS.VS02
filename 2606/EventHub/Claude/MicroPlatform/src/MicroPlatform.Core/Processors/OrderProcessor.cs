using MicroPlatform.Core.Events;

namespace MicroPlatform.Core.Processors;

public class OrderProcessor : IDisposable
{
    private readonly IMicroEventBus _bus;

    // Stored once as a field so the *same* delegate instance is used for both
    // Subscribe and Unsubscribe - required for InMemoryMicroEventBus.Unsubscribe
    // (which relies on Delegate equality) to actually find and remove it.
    private readonly Func<OrderPaid, CancellationToken, Task> _onOrderPaid;

    private bool _disposed;

    public OrderProcessor(IMicroEventBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _onOrderPaid = OnOrderPaid;
        _bus.Subscribe(_onOrderPaid);
    }

    // virtual so tests can override it without needing a separate "ForTest" flag class.
    protected virtual async Task OnOrderPaid(OrderPaid @event, CancellationToken ct)
    {
        // Business logic placeholder: in a real system this would look up the order,
        // decide on a SKU, etc. Kept intentionally simple for this reference project.
        await _bus.PublishAsync(new InventoryReserved(@event.OrderId, "DEFAULT-SKU"), ct)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _bus.Unsubscribe(_onOrderPaid);
        _disposed = true;
    }
}
