namespace Worker.Core.Infrastructure.Schedulers;

using System.Runtime.CompilerServices;
using Worker.Core.Abstractions;

public sealed class ExponentialBackoffScheduler : ISchedulerStrategy
{
    private readonly IClock _clock;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly int _maxAttempts;
    private readonly Random _jitter = new();
    private int _attempt;

    public ExponentialBackoffScheduler(IClock clock, TimeSpan baseDelay, TimeSpan maxDelay, int maxAttempts = 10)
    {
        _clock = clock;
        _baseDelay = baseDelay;
        _maxDelay = maxDelay;
        _maxAttempts = maxAttempts;
    }

    public async IAsyncEnumerable<DateTimeOffset> GetScheduleAsync([EnumeratorCancellation] CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _attempt < _maxAttempts)
        {
            var delay = CalculateNextDelay();
            var nextRunAt = _clock.UtcNow.Add(delay);

            var waitTime = nextRunAt - _clock.UtcNow;
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, stoppingToken);
            }

            yield return nextRunAt;
            _attempt++;
        }
    }

    private TimeSpan CalculateNextDelay()
    {
        double ticks = Math.Min(_baseDelay.Ticks * Math.Pow(2, _attempt), _maxDelay.Ticks);
        var jitteredTicks = (long)(ticks * _jitter.NextDouble());
        return TimeSpan.FromTicks(jitteredTicks);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
