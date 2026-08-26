// TradingWorker.cs
using Trading.Core;
using Trading.Processors;
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

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая отмена при остановке
            _logger.LogInformation("Ожидаемая отмена при остановке. Trading cycle stopping ...");
        }
        finally
        {
            _logger.LogInformation("Trading cycle stopped gracefully.");
        }
    }
}
