using FractalCell02.Core.Interfaces;

namespace FractalCell02;

public record FractalEvent(
    string EventId,
    DateTime Timestamp,
    string SourceCellId,
    string TargetCellId,
    string EventType,
    object Payload
) : IApplicationEvent;
