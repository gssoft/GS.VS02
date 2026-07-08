using Microsoft.Extensions.Logging;

namespace DS.BackgroundServices.Core04;

public class GenericBackgroundService<TTask, TSchedule, TTune> : BackgroundServiceBase
    where TTask : IBackgroundTask
    where TSchedule : IBackgroundSchedule
    where TTune : IBackgroundTuning
{
    private readonly TTask _task;
    private readonly TSchedule _schedule;
    private readonly TTune _tuning;
    private readonly ILogger _logger;

    public GenericBackgroundService(
        TTask task,
        TSchedule schedule,
        TTune tuning,
        ILogger<GenericBackgroundService<TTask, TSchedule, TTune>> logger)
        : base(logger)
    {
        _task = task;
        _schedule = schedule;
        _tuning = tuning;
        _logger = logger;
    }

    protected override async Task ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Запуск {ScheduleName} + {TaskName} с юстировкой {TuningName}",
            typeof(TSchedule).Name, typeof(TTask).Name, typeof(TTune).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            var waitTask = _schedule.WaitForNextAsync(stoppingToken);
            if (waitTask is null)
            {
                _logger.LogDebug("Расписание вернуло null, выход из цикла");
                break;
            }

            try
            {
                await waitTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            // Выполняем задачу через слой юстировки
            await _tuning.ExecuteWithTuningAsync(
                ct => _task.ExecuteAsync(ct),
                stoppingToken).ConfigureAwait(false);
        }
    }
}
