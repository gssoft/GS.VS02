// Interfaces/IApplicationEvent.cs
public interface IApplicationEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
}
