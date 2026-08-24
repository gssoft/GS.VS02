using MassTransit;
using MicroPlatform.Core.Events;

namespace MicroPlatform.MassTransitHost.Consumers;

public class InventoryProcessor : IConsumer<InventoryReserved>
{
    public Task Consume(ConsumeContext<InventoryReserved> context)
    {
        Console.WriteLine(
            $"[InventoryProcessor] Reserved sku '{context.Message.Sku}' for order {context.Message.OrderId}");
        return Task.CompletedTask;
    }
}
