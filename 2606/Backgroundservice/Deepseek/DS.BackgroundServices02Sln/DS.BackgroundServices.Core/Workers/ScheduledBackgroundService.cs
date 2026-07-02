using Cronos;
using DS.BackgroundServices.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DS.BackgroundServices.Core.Workers;

/// <summary>
/// Выполняет задачу строго по cron‑расписанию. Требует пакет Cronos.
/// </summary>
public abstract class ScheduledBackgroundService : BackgroundServiceBase
{
    private readonly CronExpression _cronExpression;
    private readonly ILogger _logger;

    protected ScheduledBackgroundService(ILogger logger, string cronExpression)
        : base(logger)
    {
        _cronExpression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
        _logger = logger;
    }

    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = _cronExpression.GetNextOccurrence(now, inclusive: false);
            if (next is null)
                break; // расписание никогда не сработает

            var delay = next.Value - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }

            // Если остановка запрошена во время задержки – выходим
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await DoWorkAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении задания по расписанию в {ServiceName}", GetType().Name);
            }
        }
    }
}
