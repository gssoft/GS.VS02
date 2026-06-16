// Handlers/HandlerB.cs
using WorkerEventBus.Events;

namespace WorkerEventBus.Handlers;

public class HandlerB : IEventHandler<EventB>
{
    private readonly ILogger<HandlerB> _logger;

    public HandlerB(ILogger<HandlerB> logger)
    {
        _logger = logger;
    }

    // HandlerB.cs
    public async Task HandleAsync(EventB @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔵 HandlerB started for event {@Event}", @event);
        await Task.Delay(75, cancellationToken); // ✅ 150ms вместо 1500ms
        _logger.LogInformation("✅ HandlerB completed: Number={Number}", @event.Number);
    }  
}
