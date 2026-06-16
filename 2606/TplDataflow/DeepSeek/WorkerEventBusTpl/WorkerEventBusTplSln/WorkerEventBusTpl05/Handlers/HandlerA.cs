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

    // HandlerA.cs
    public async Task HandleAsync(EventA @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🟢 HandlerA started for event {@Event}", @event);
        await Task.Delay(50, cancellationToken); // ✅ 100ms вместо 1000ms
        _logger.LogInformation("✅ HandlerA completed: {Data}", @event.Data);
    }
}
