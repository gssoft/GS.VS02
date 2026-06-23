// Универсальный класс события для фрактальной архитектуры
public class FractalEvent : IApplicationEvent
{
    public string EventId { get; }
    public DateTime Timestamp { get; }
    public string SourceBlockId { get; }
    public string TargetBlockId { get; }
    public string EventType { get; }
    public object? Data { get; } // Для строгой типизации используйте FractalEvent<TData>

    public FractalEvent(
        string eventId,
        DateTime timestamp,
        string sourceBlockId,
        string targetBlockId,
        string eventType,
        object? data = null)
    {
        EventId = eventId;
        Timestamp = timestamp;
        SourceBlockId = sourceBlockId;
        TargetBlockId = targetBlockId;
        EventType = eventType;
        Data = data;
    }
}
