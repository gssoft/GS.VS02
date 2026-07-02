using DS.BackgroundServices.Core02;
using Microsoft.Extensions.Logging;

/// <summary>Задача по Cron‑расписанию (каждые 10 секунд).</summary>
public class CronJob : ScheduledBackgroundService
{
    private readonly ILogger<CronJob> _logger;
    public CronJob(ILogger<CronJob> logger)
        : base(logger, "*/10 * * * * *")
    {
        _logger = logger;

    } // Cronos формат с секундами

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CronJob: запуск по расписанию в {Time}", DateTime.UtcNow);
        await Task.Delay(300, stoppingToken);
    }
}
