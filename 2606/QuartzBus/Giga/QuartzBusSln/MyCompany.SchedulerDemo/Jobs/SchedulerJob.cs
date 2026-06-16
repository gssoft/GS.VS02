// Jobs/SchedulerJob.cs

using MassTransit;
using MyCompany.SchedulerDemo.Messages;
using Quartz;

namespace MyCompany.SchedulerDemo.Jobs;

public class SchedulerJob : IJob
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SchedulerJob> _logger;

    public SchedulerJob(IPublishEndpoint publishEndpoint, ILogger<SchedulerJob> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // Логика: в зависимости от имени триггера публикуем нужный сигнал.
        var triggerName = context.Trigger.Key.Name;
        _logger.LogInformation("Quartz job triggered by {TriggerName}", triggerName);

        if (triggerName == "MorningStartTrigger")
        {
            await _publishEndpoint.Publish(new StartWorkingSignal());
        }
        else if (triggerName == "EveningStopTrigger")
        {
            await _publishEndpoint.Publish(new StopWorkingSignal());
        }
    }
}
