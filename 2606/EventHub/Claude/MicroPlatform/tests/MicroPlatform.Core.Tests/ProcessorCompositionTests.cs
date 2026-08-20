using MicroPlatform.Core;
using MicroPlatform.Core.Events;
using MicroPlatform.Core.Processors;
using MicroPlatform.Core.Tests.TestSupport;
using Xunit;

namespace MicroPlatform.Core.Tests;

public class ProcessorCompositionTests
{
    [Fact]
    public async Task OrderPaid_CausesOrderProcessorToPublishInventoryReserved()
    {
        var recordingBus = new RecordingMicroEventBus(new InMemoryMicroEventBus());
        using var orderProcessor = new OrderProcessor(recordingBus);

        var orderId = Guid.NewGuid();
        await recordingBus.PublishAsync(new OrderPaid(orderId, 150.00m));

        Assert.Contains(
            recordingBus.PublishedEvents,
            e => e is InventoryReserved reserved && reserved.OrderId == orderId);
    }

    [Fact]
    public async Task OrderPaid_PropagatesEndToEndToInventoryProcessor()
    {
        var bus = new InMemoryMicroEventBus();
        using var orderProcessor = new OrderProcessor(bus);
        using var inventoryProcessor = new ObservableInventoryProcessor(bus);

        var orderId = Guid.NewGuid();
        await bus.PublishAsync(new OrderPaid(orderId, 200m));

        Assert.NotNull(inventoryProcessor.LastReceived);
        Assert.Equal(orderId, inventoryProcessor.LastReceived!.OrderId);
        Assert.Equal("DEFAULT-SKU", inventoryProcessor.LastReceived.Sku);
    }

    // Test double: overrides the virtual handler to observe what it received,
    // instead of pretending to be a subscription that never fires (see the
    // earlier "{ClassName}ForTest" bugs discussed in this conversation).
    private sealed class ObservableInventoryProcessor : InventoryProcessor
    {
        public InventoryReserved? LastReceived { get; private set; }

        public ObservableInventoryProcessor(IMicroEventBus bus) : base(bus)
        {
        }

        protected override Task OnInventoryReserved(InventoryReserved @event, CancellationToken ct)
        {
            LastReceived = @event;
            return Task.CompletedTask;
        }
    }
}
