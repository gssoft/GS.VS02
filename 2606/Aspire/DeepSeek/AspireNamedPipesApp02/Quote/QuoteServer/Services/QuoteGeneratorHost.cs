// Services/QuoteGeneratorHost.cs
using QuoteServer.Helpers;
using QuoteServer.Models;

namespace QuoteServer.Services;

public class QuoteGeneratorHost : BackgroundService
{
    private readonly EventHub _eventHub;
    private readonly ILogger<QuoteGeneratorHost> _logger;

    // Соответствие тикер -> канал
    private static readonly Dictionary<string, string> TickerToChannel = new()
    {
        ["GOOGL"] = "tech",
        ["MSFT"] = "tech",
        ["NVDA"] = "tech",
        ["AMZN"] = "consumer",
        ["AAPL"] = "consumer",
        ["JPM"] = "finance",
        ["BAC"] = "finance",
        ["GS"] = "finance",
        ["XOM"] = "energy",
        ["CVX"] = "energy"
    };

    public QuoteGeneratorHost(EventHub eventHub, ILogger<QuoteGeneratorHost> logger)
    {
        _eventHub = eventHub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quote generator started.");
        var rnd = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var channel = TickerToChannel.GetValueOrDefault(quote.Ticker, "unknown");

            _logger.LogInformation("[{Channel}] {Ticker} @ {Last:C}", channel, quote.Ticker, quote.Last);
            await _eventHub.PublishToChannelAsync(channel, quote, stoppingToken);

            await Task.Delay(rnd.Next(500, 1500), stoppingToken);
        }
    }
}
