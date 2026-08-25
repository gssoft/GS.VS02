// TradingWorker.cs
using Trading.Core;
using Trading.Processors;
using Trading.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Trading.App;

public class TradingWorker : BackgroundService
{
    private readonly IMicroEventBus _bus;
    private readonly QuotesFeederProcessor _quotesFeeder;
    private readonly ILogger<TradingWorker> _logger;

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

        // Периодически генерируем котировки
        while (!stoppingToken.IsCancellationRequested)
        {
            await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}

