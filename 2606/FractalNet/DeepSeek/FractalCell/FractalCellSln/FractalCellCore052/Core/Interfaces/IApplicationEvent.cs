// Core/Interfaces/IApplicationEvent.cs

namespace FractalCellCore.Core.Interfaces;

public interface IApplicationEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
}
