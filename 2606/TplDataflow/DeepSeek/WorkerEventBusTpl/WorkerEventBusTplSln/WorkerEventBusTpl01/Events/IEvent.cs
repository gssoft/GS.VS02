// Events/IEvent.cs
namespace WorkerEventBus.Events;

public interface IEvent
{
    string Id { get; }
    DateTime OccurredOn { get; }
}
