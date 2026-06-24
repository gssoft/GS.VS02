using FractalCell.Core.Interfaces;

namespace FractalCell;

public record FractalEvent(
    string EventId,
    DateTime Timestamp,
    string SourceCellId,
    string TargetCellId,
    string EventType,
    object Payload
) : IApplicationEvent;
