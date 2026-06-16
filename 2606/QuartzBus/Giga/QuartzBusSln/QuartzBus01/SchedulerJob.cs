using MassTransit;
using Quartz;

public class SchedulerJob : IJob
{
    private readonly IPublishEndpoint _publishEndpoint;

    public SchedulerJob(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // Логика может быть любой. Например, по типу триггера посылаем разные сигналы.
        var jobKey = context.JobDetail.Key.Name;

        if (jobKey == "MorningStartTrigger")
        {
            await _publishEndpoint.Publish(new StartWorkingSignal());
        }
        else if (jobKey == "EveningStopTrigger")
        {
            await _publishEndpoint.Publish(new StopWorkingSignal());
        }
    }
}

