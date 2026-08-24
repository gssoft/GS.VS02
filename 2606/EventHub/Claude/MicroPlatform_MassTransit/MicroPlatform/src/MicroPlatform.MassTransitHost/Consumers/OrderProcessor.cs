using MassTransit;
using MicroPlatform.Core.Events;

namespace MicroPlatform.MassTransitHost.Consumers;

/// <summary>
/// MassTransit equivalent of the hand-rolled OrderProcessor. Same shape trade-off as
/// discussed for Rebus: no runtime Subscribe/Unsubscribe - AddConsumer + ConfigureEndpoints
/// wires the subscription topology once, at startup. Unlike the Rebus version, this uses
/// real Publish/Subscribe (not a Send-to-single-queue workaround) because MassTransit's
/// ConfigureEndpoints automatically creates a receive endpoint per consumer and subscribes
/// it to the message types that consumer handles - true pub/sub, no extra ceremony, on both
/// the in-memory and RabbitMQ transports.
/// </summary>
public class OrderProcessor : IConsumer<OrderPaid>
{
    public async Task Consume(ConsumeContext<OrderPaid> context)
    {
        // context.Publish (rather than a plain IPublishEndpoint) keeps this message's
        // CorrelationId/conversation tracking linked to the OrderPaid that triggered it -
        // useful for tracing a chain of events later, something our hand-rolled bus never had.
        await context.Publish(new InventoryReserved(context.Message.OrderId, "DEFAULT-SKU"));
    }
}
