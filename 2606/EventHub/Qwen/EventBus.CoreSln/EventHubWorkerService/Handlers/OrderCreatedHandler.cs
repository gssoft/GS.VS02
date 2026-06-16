// Handlers/OrderCreatedHandler.cs

using EventBus.Abstractions;
using EventHubWorkerService.Messages;

using Microsoft.Extensions.Logging;

namespace EventHubWorkerService.Handlers;

public class OrderCreatedHandler : IHandler<OrderCreatedEvent>
{
    private readonly ILogger<MyEventHandler> _logger;

    public OrderCreatedHandler(ILogger<MyEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("✅ [Handler] Received event: {Id} | {Payload}",
            @event.OrderId, @event.Amount);

        // Имитация асинхронной работы (БД, сеть и т.д.)
        // Обратите внимание: это выполняется в контексте MyAnalyticsService!
        return Task.Delay(100, cancellationToken);
    }
}
