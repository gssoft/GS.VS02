using DS.BackgroundServices.Core02;
using Microsoft.Extensions.Logging;

// < summary > Периодическая обработка каждые 5 секунд.</summary>
public class PeriodicProcessor : PeriodicBackgroundService
{
    private readonly ILogger<PeriodicProcessor> _logger;

    public PeriodicProcessor(ILogger<PeriodicProcessor> logger)
        : base(logger, TimeSpan.FromSeconds(5))
    {
        _logger = logger;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeriodicProcessor: выполняю работу...");
        await Task.Delay(200, stoppingToken); // имитация полезной нагрузки
    }
}
