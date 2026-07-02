using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GA.BackgroundServices.Core;

public class CustomDataProcessor : WorkerCore
{
    public CustomDataProcessor(ILogger<CustomDataProcessor> logger) : base(logger) { }

    protected override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        // Здесь ваша бизнес-логика
        _logger.LogInformation("DoWorkAsync(): Выполняется обработка данных...");
        return Task.CompletedTask;
    }

    protected override async Task OnStartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OnStartingAsync(): Инициализация соединений перед запуском...");
        await base.OnStartingAsync(cancellationToken);
    }

    protected override async Task OnStoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OnStoppedAsync(): Закрытие соединений БД...");
        await base.OnStoppedAsync(cancellationToken);
    }
}
