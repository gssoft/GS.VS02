using QuotesApp.Shared.Models;

namespace QuotesApp.PipeServer.Services;

public static class QuoteGenerator
{
    private static readonly Random _rnd = new();

    private static readonly Dictionary<string, double> BasePrices = new()
    {
        ["GOOGL"] = 178.50, ["MSFT"] = 425.30, ["NVDA"] = 890.75,
        ["AMZN"] = 185.20, ["AAPL"] = 232.40,
        ["JPM"] = 205.60, ["BAC"] = 42.80, ["GS"] = 478.90,
        ["XOM"] = 118.30, ["CVX"] = 162.50
    };

    public static StockQuote Generate(string ticker, string portfolio)
    {
        var basePrice = BasePrices.GetValueOrDefault(ticker, 100.0);
        var change = (_rnd.NextDouble() - 0.5) * basePrice * 0.02;
        var newPrice = basePrice + change;
        var spread = Math.Round(newPrice * 0.001, 2);

        return new StockQuote(
            Ticker: ticker,
            Bid: Math.Round(newPrice - spread, 2),
            Ask: Math.Round(newPrice + spread, 2),
            Last: Math.Round(newPrice, 2),
            Volume: _rnd.Next(100, 50000),
            Portfolio: portfolio,
            Timestamp: DateTime.UtcNow
        );
    }
}
