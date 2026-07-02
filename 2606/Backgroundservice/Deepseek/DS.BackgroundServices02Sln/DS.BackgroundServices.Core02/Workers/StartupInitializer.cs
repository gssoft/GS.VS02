using DS.BackgroundServices.Core02;
using Microsoft.Extensions.Logging;

/// <summary>Одноразовое задание при запуске.</summary>
public class StartupInitializer : OneTimeStartupService
{
    private readonly ILogger<StartupInitializer> _logger;
    public StartupInitializer(ILogger<StartupInitializer> logger) : base(logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteOnceAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StartupInitializer: выполняю разовую инициализацию...");
        await Task.Delay(1000, stoppingToken);
        _logger.LogInformation("StartupInitializer: инициализация завершена.");
    }
}
