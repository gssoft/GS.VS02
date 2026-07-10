using FractalCellCore.Core.Interfaces;

namespace FractalCellCore;

/// <summary>
/// Событие, несущее фрактальное время
/// </summary>
public record FractalTimeEvent : IApplicationEvent
{
    public string EventId { get; init; }
    public DateTime Timestamp { get; init; }          // время создания события (UTC)
    public string SourceCellId { get; init; }
    public string TargetCellId { get; init; } = string.Empty; // broadcast
    public string EventType => "FractalTime";

    // Специфичные поля
    public DateTimeOffset FractalTime { get; init; }   // само фрактальное время
    public long Tick { get; init; }                    // порядковый номер тика
    public bool IsSynchronized { get; init; }          // синхронизировано ли с внешним эталоном

    public FractalTimeEvent(
        string eventId,
        DateTime timestamp,
        string sourceCellId,
        DateTimeOffset fractalTime,
        long tick,
        bool isSynchronized = true,
        string targetCellId = "")
    {
        EventId = eventId;
        Timestamp = timestamp;
        SourceCellId = sourceCellId;
        TargetCellId = targetCellId;
        FractalTime = fractalTime;
        Tick = tick;
        IsSynchronized = isSynchronized;
    }
}

