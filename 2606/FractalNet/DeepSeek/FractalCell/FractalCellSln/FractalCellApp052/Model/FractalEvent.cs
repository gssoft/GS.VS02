
using FractalCellCore.Core.Interfaces;

namespace FractalCellApp;

/// <summary>
/// Базовое событие для фрактальной системы
/// </summary>
public record FractalEvent : IApplicationEvent
{
    public string EventId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string SourceCellId { get; init; } = string.Empty;
    public string TargetCellId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public object Payload { get; init; } = new();

    public FractalEvent() { }

    public FractalEvent(
        string eventId,
        DateTime timestamp,
        string sourceCellId,
        string targetCellId,
        string eventType,
        object payload)
    {
        EventId = eventId;
        Timestamp = timestamp;
        SourceCellId = sourceCellId;
        TargetCellId = targetCellId;
        EventType = eventType;
        Payload = payload;
    }
}

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
