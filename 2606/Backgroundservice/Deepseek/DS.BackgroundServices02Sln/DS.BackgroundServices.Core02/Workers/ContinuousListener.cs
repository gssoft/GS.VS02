using DS.BackgroundServices.Core02;
using Microsoft.Extensions.Logging;

/// <summary>Непрерывный конвейер (слушает очередь/канал).</summary>
public class ContinuousListener : ContinuousBackgroundService
{
    private readonly ILogger<ContinuousListener> _logger;
    public ContinuousListener(ILogger<ContinuousListener> logger) : base(logger)
    {
        _logger = logger;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContinuousListener: ожидаю новые данные...");
        // В реальном коде здесь ожидание сообщения из канала/сокета
        await Task.Delay(2000, stoppingToken);
    }
}
