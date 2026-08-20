using MicroPlatform.Core;
using MicroPlatform.Core.Events;
using MicroPlatform.Core.Processors;

var bus = new InMemoryMicroEventBus();

using var orderProcessor = new OrderProcessor(bus);
using var inventoryProcessor = new InventoryProcessor(bus);

var orderId = Guid.NewGuid();
Console.WriteLine($"Publishing OrderPaid for order {orderId}...");

await bus.PublishAsync(new OrderPaid(orderId, 150.00m));

Console.WriteLine("Done.");
