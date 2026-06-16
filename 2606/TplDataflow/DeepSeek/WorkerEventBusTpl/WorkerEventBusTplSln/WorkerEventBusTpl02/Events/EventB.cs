// Events/EventB.cs
namespace WorkerEventBus.Events;

public record EventB(int Number) : IEvent
{
    public string Id => Guid.NewGuid().ToString();
    public DateTime OccurredOn => DateTime.UtcNow;
}

