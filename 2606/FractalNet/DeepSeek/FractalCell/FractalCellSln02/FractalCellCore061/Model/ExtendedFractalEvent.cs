// Model/ExtendedFractalEvent.cs

namespace FractalCellCore;

/// <summary>
/// Расширенное событие с дополнительными метаданными
/// </summary>
public record ExtendedFractalEvent : FractalEvent
{
    public string? CorrelationId { get; init; }
    public int RetryCount { get; init; } = 0;
    public Dictionary<string, object> Metadata { get; init; } = new();

    public ExtendedFractalEvent(
        string eventId,
        DateTime timestamp,
        string sourceCellId,
        string targetCellId,
        string eventType,
        object payload,
        string? correlationId = null,
        int retryCount = 0,
        Dictionary<string, object>? metadata = null)
        : base(eventId, timestamp, sourceCellId, targetCellId, eventType, payload)
    {
        CorrelationId = correlationId;
        RetryCount = retryCount;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}
