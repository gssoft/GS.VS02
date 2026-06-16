using QuotesServer.Models;

namespace QuotesServer.Helpers;

public static class QuoteGenerator
{
    private static readonly Random rnd = new();
    private static readonly string[] tickers = { "GOOGL", "AMZN", "MSFT", "AAPL", "NVDA" };

    public static StockQuote Generate()
    {
        var ticker = tickers[rnd.Next(tickers.Length)];
        var basePrice = (decimal)(rnd.Next(100, 2000) + rnd.NextDouble());
        var spread = (decimal)(rnd.NextDouble() * 2);

        return new StockQuote(
            Timestamp: DateTime.Now,
            Ticker: ticker,
            Bid: Math.Round(basePrice - spread, 2),
            Ask: Math.Round(basePrice + spread, 2),
            Last: Math.Round(basePrice, 2),
            Volume: rnd.Next(1, 1000));
    }
}

