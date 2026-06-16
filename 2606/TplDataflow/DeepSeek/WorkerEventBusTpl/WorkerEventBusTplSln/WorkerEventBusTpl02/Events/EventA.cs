// Events/EventA.cs
namespace WorkerEventBus.Events;

public record EventA(string Data) : IEvent
{
    public string Id => Guid.NewGuid().ToString();
    public DateTime OccurredOn => DateTime.UtcNow;
}
