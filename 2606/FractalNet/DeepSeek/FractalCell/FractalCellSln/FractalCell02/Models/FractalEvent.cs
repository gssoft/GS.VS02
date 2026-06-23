// Model/FractalEvent.cs - событие для перехода между ячейками фрактала
public record FractalEvent(
    string EventId,
    DateTime Timestamp,
    string SourceCellId,
    string TargetCellId,
    string EventType,
    object Payload
) : IApplicationEvent;

