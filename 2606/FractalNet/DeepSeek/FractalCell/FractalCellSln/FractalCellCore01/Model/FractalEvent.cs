// Model/FractalEvent.cs

using FractalCellCore.Core.Interfaces;

namespace FractalCellCore;

public record FractalEvent(
    string EventId,
    DateTime Timestamp,
    string SourceCellId,
    string TargetCellId,
    string EventType,
    object Payload
) : IApplicationEvent;
