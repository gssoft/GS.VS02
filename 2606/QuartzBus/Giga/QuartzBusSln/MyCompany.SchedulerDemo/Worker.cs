using MassTransit;
using MyCompany.SchedulerDemo.Messages;

public class MyBusinessWorker : IConsumer<StartWorkingSignal>, IConsumer<StopWorkingSignal>
{
    private readonly ILogger<MyBusinessWorker> _logger;
    private bool _isRunning = false;
    private readonly string _folderPath = @"D:\WorkFolder";

    public Task Consume(ConsumeContext<StartWorkingSignal> context)
    {
        _logger.LogInformation("Received START signal. Starting work...");
        _isRunning = true;

        // Запускаем цикл обработки в отдельной задаче
        Task.Run(async () =>
        {
            while (_isRunning)
            {
                // Реальная логика: найти файлы и обработать их
                var files = Directory.GetFiles(_folderPath);
                foreach (var file in files)
                {
                    _logger.LogInformation("Processing file: {File}", file);
                    // ... логика обработки ...
                }

                // Ждем некоторое время перед следующей проверкой
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            _logger.LogInformation("Worker loop has stopped.");
        });

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<StopWorkingSignal> context)
    {
        _logger.LogInformation("Received STOP signal. Stopping work...");
        _isRunning = false; // Это безопасно остановит цикл в методе Start
        return Task.CompletedTask;
    }
}

//namespace MyCompany.SchedulerDemo
//{
//    public class Worker(ILogger<Worker> logger) : BackgroundService
//    {
//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                if (logger.IsEnabled(LogLevel.Information))
//                {
//                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//                }
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//    }
//}
