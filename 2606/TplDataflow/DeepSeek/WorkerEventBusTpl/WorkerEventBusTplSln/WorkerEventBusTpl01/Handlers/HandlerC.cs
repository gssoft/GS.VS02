// Handlers/HandlerC.cs
using WorkerEventBus.Events;

namespace WorkerEventBus.Handlers;

public class HandlerC : IEventHandler<EventC>
{
    private readonly ILogger<HandlerC> _logger;

    public HandlerC(ILogger<HandlerC> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(EventC @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🟡 HandlerC started for event {@Event}", @event);
        await Task.Delay(800, cancellationToken);
        _logger.LogInformation("✅ HandlerC completed: Flag={Flag}", @event.Flag);
    }
}
