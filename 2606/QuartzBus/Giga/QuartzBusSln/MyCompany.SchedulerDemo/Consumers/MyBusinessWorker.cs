// Consumers/MyBusinessWorker.cs

using MassTransit;
using MyCompany.SchedulerDemo.Messages;

namespace MyCompany.SchedulerDemo.Consumers;

public class MyBusinessWorker :
    IConsumer<StartWorkingSignal>,
    IConsumer<StopWorkingSignal>
{
    private readonly ILogger<MyBusinessWorker> _logger;
    private bool _isRunning = false; // Флаг для имитации работы

    public MyBusinessWorker(ILogger<MyBusinessWorker> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<StartWorkingSignal> context)
    {
        _logger.LogInformation("Received START signal. Starting work...");
        _isRunning = true;
        // Здесь была бы логика реального старта, например,
        // запуск цикла обработки данных.
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<StopWorkingSignal> context)
    {
        _logger.LogInformation("Received STOP signal. Stopping work...");
        _isRunning = false;
        // Здесь была бы логика корректной остановки и очистки.
        return Task.CompletedTask;
    }
}
