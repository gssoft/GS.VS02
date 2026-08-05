using QuotesApp.ProviderCore.Abstractions;
using QuotesApp.Shared.Models;

namespace QuotesApp.Providers.Simulator;

public class SimulatorQuoteFetcher : IQuoteFetcher
{
    private static readonly Random _rnd = new();
    private static readonly Dictionary<string, double> BasePrices = new()
    {
        ["GOOGL"] = 178.50,
        ["MSFT"] = 425.30,
        ["NVDA"] = 890.75,
        ["AMZN"] = 185.20,
        ["AAPL"] = 232.40
    };

    public Task<StockQuote> FetchAsync(string ticker, string portfolio, CancellationToken ct)
    {
        var basePrice = BasePrices.GetValueOrDefault(ticker, 100.0);
        var change = (_rnd.NextDouble() - 0.5) * basePrice * 0.02;
        var newPrice = basePrice + change;
        var spread = Math.Round(newPrice * 0.001, 2);

        var quote = new StockQuote(
            Ticker: ticker,
            Bid: Math.Round(newPrice - spread, 2),
            Ask: Math.Round(newPrice + spread, 2),
            Last: Math.Round(newPrice, 2),
            Volume: _rnd.Next(100, 50000),
            Portfolio: portfolio,
            Timestamp: DateTime.UtcNow
        );

        return Task.FromResult(quote);
    }
}
