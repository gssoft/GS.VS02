namespace FractalCell.Core.Interfaces;

public interface IApplicationEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
}
