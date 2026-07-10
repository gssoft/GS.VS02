namespace Worker.Core.Infrastructure.Hosting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Core.Abstractions;

public sealed class GenericJobHost(
    ISchedulerStrategy scheduler,
    IBehaviorStrategy behavior,
    ILivenessStrategy liveness,
    ILogger<GenericJobHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GenericJobHost starting.");

        try
        {
            await foreach (var scheduledStart in scheduler.GetScheduleAsync(stoppingToken))
            {
                using var workCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                // Таймаут выполнения одной итерации (защита от "залипания" воркера)
                workCts.CancelAfter(TimeSpan.FromMinutes(5));

                try
                {
                    await behavior.ExecuteAsync(workCts.Token);
                    liveness.Pulse(); // Оповестили мир, что мы живы после успешного прохода
                }
                catch (OperationCanceledException) when (workCts.Token.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning("Work iteration timed out after 5 minutes.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during job execution at {Time}", DateTimeOffset.Now);
                    // Не пробрасываем исключение вверх, чтобы не убить хост
                }
            }
        }
        finally
        {
            await scheduler.DisposeAsync();
        }

        logger.LogInformation("GenericJobHost is stopping.");
    }
}
