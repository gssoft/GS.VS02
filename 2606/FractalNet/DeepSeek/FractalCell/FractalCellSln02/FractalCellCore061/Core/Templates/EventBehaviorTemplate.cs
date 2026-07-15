using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Templates;

public abstract class EventBehaviorTemplate<TEvent> : BehaviorTemplate
    where TEvent : IApplicationEvent
{
    protected EventBehaviorTemplate(ILogger? logger = null)
        : base(logger)
    {
    }

    public override Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        return Task.FromResult(@event is TEvent);
    }

    public override async Task HandleAsync(IApplicationEvent @event)
    {
        if (@event is TEvent typed)
            await HandleEventAsync(typed);
    }

    protected abstract Task HandleEventAsync(TEvent @event);
}

