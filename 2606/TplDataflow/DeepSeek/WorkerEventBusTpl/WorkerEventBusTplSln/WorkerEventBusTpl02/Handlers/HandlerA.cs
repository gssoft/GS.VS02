// Handlers/HandlerA.cs
using WorkerEventBus.Events;

namespace WorkerEventBus.Handlers;

public class HandlerA : IEventHandler<EventA>
{
    private readonly ILogger<HandlerA> _logger;

    public HandlerA(ILogger<HandlerA> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(EventA @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🟢 HandlerA started for event {@Event}", @event);
        await Task.Delay(1000, cancellationToken); // Имитация работы
        _logger.LogInformation("✅ HandlerA completed: {Data}", @event.Data);
    }
}
