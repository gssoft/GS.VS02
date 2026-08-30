using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.Core;
using Trading.Processors;

namespace Trading.App;

public class TradingWorker : BackgroundService
{
    private readonly IMicroEventBus _bus;
    private readonly QuotesFeederProcessor _quotesFeeder;
    private readonly ILogger<TradingWorker> _logger;

    private readonly int LoopTimeoutInSec = 2;
    private readonly int StopTimeoutInSec = 5;

    public TradingWorker(
        IMicroEventBus bus,
        QuotesFeederProcessor quotesFeeder,
        ILogger<TradingWorker> logger)
    {
        _bus = bus;
        _quotesFeeder = quotesFeeder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trading cycle started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(LoopTimeoutInSec), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Trading cycle stopped by cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in trading cycle. Retrying in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(StopTimeoutInSec), stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Trading cycle stopped gracefully.");
        }
    }
}