using MicroPlatform.Core.Events;

namespace MicroPlatform.Core.Processors;

public class InventoryProcessor : IDisposable
{
    private readonly IMicroEventBus _bus;
    private readonly Func<InventoryReserved, CancellationToken, Task> _onInventoryReserved;
    private bool _disposed;

    public InventoryProcessor(IMicroEventBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _onInventoryReserved = OnInventoryReserved;
        _bus.Subscribe(_onInventoryReserved);
    }

    protected virtual Task OnInventoryReserved(InventoryReserved @event, CancellationToken ct)
    {
        Console.WriteLine($"[InventoryProcessor] Reserved sku '{@event.Sku}' for order {@event.OrderId}");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _bus.Unsubscribe(_onInventoryReserved);
        _disposed = true;
    }
}
