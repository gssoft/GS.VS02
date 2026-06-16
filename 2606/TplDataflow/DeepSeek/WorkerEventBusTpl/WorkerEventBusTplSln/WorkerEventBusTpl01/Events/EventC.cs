// Events/EventC.cs
namespace WorkerEventBus.Events;

public record EventC(bool Flag) : IEvent
{
    public string Id => Guid.NewGuid().ToString();
    public DateTime OccurredOn => DateTime.UtcNow;
}
