namespace Worker.Core.Infrastructure.Liveness;

using System.Threading;
using Worker.Core.Abstractions;

public sealed class DefaultLivenessStrategy : ILivenessStrategy
{
    private long _lastPulseTicks = DateTimeOffset.UtcNow.Ticks;

    public void Pulse() =>
        Interlocked.Exchange(ref _lastPulseTicks, DateTimeOffset.UtcNow.Ticks);

    // Этот метод можно использовать в Health Checks провайдере
    public TimeSpan GetStaleness() =>
        TimeSpan.FromTicks(DateTimeOffset.UtcNow.Ticks - Volatile.Read(ref _lastPulseTicks));
}
