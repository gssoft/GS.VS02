namespace Worker.Core.Abstractions;

public interface ISchedulerStrategy : IAsyncDisposable
{
    IAsyncEnumerable<DateTimeOffset> GetScheduleAsync(CancellationToken stoppingToken);
}
