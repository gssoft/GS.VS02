namespace FractalCell02.Core.Interfaces;

public interface IApplicationEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
}
