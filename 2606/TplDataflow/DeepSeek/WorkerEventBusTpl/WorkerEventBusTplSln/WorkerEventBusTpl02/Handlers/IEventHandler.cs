// Handlers/IEventHandler.cs

using WorkerEventBus.Events;

namespace WorkerEventBus.Handlers;

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
