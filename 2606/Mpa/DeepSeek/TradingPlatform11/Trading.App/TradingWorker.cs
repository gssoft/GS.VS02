// 26.08.30

using Microsoft.Extensions.Configuration;
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
    private readonly int _loopTimeoutInSec;
    private readonly int _stopTimeoutInSec;

    public TradingWorker(
        IMicroEventBus bus,
        QuotesFeederProcessor quotesFeeder,
        ILogger<TradingWorker> logger,
        IConfiguration configuration)
    {
        _bus = bus;
        _quotesFeeder = quotesFeeder;
        _logger = logger;
        _loopTimeoutInSec = configuration.GetValue<int>("Trading:LoopTimeoutInSec", 2);
        _stopTimeoutInSec = configuration.GetValue<int>("Trading:StopTimeoutInSec", 5);
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
                    await Task.Delay(TimeSpan.FromSeconds(_loopTimeoutInSec), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Trading cycle stopped by cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in trading cycle. Retrying in {StopTimeout} seconds...", _stopTimeoutInSec);
                    await Task.Delay(TimeSpan.FromSeconds(_stopTimeoutInSec), stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Trading cycle stopped gracefully.");
        }
    }
}

