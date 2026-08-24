using MassTransit.Testing;
using MicroPlatform.Core.Events;
using MicroPlatform.MassTransitHost.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MicroPlatform.MassTransitHost.Tests;

public class ConsumerCompositionTests
{
    [Fact]
    public async Task OrderPaid_IsConsumedByOrderProcessor_AndPublishesInventoryReserved()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<OrderProcessor>();
                cfg.AddConsumer<InventoryProcessor>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();

        // Documented pattern: resolve IPublishEndpoint from harness.Scope, not from
        // the outer provider directly, so the message flows through the test harness's
        // instrumentation (that's what makes harness.Consumed/Published/GetConsumerHarness
        // able to see it).
        await harness.Scope.ServiceProvider.GetRequiredService<MassTransit.IPublishEndpoint>()
            .Publish(new OrderPaid(orderId, 150.00m));

        Assert.True(await harness.Consumed.Any<OrderPaid>());
        Assert.True(await harness.Published.Any<InventoryReserved>());

        var inventoryHarness = harness.GetConsumerHarness<InventoryProcessor>();
        Assert.True(await inventoryHarness.Consumed.Any<InventoryReserved>(
            m => m.Context.Message.OrderId == orderId));
    }
}
