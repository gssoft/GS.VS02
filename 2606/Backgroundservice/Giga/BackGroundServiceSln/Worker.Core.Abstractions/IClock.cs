namespace Worker.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
