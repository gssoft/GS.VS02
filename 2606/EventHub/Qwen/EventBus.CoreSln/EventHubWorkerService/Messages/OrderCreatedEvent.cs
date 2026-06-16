// Messages/OrderCreatedEvent.cs

namespace EventHubWorkerService.Messages;

public record OrderCreatedEvent(Guid OrderId, decimal Amount);
