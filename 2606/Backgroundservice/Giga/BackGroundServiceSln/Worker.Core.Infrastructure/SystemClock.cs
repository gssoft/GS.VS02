namespace Worker.Core.Infrastructure.Clocks;

using Worker.Core.Abstractions;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
