// Helpers/QuoteGenerator.cs

// QuoteServer.Models

using QuoteServer.Models;

namespace QuoteServer.Helpers;

public static class QuoteGenerator
{
    private static readonly Random rnd = new();

    // Все доступные тикеры
    private static readonly string[] tickers =
    { 
        // Tech Stocks
        "GOOGL", "MSFT", "NVDA",
        // Consumer Stocks
        "AMZN", "AAPL",
        // Finance Stocks
        "JPM", "BAC", "GS",
        // Energy Stocks
        "XOM", "CVX"
    };

    public static StockQuote Generate()
    {
        var ticker = tickers[rnd.Next(tickers.Length)];

        // Генерируем разные цены для разных типов акций
        decimal basePrice;
        if (ticker == "GOOGL" || ticker == "AMZN" || ticker == "NVDA")
        {
            // Дорогие технологические акции
            basePrice = (decimal)(rnd.Next(1000, 3000) + rnd.NextDouble());
        }
        else if (ticker == "JPM" || ticker == "GS")
        {
            // Финансовые акции
            basePrice = (decimal)(rnd.Next(200, 500) + rnd.NextDouble());
        }
        else
        {
            // Обычные акции
            basePrice = (decimal)(rnd.Next(50, 500) + rnd.NextDouble());
        }

        var spread = (decimal)(rnd.NextDouble() * 2);

        return new StockQuote(
            Timestamp: DateTime.Now,
            Ticker: ticker,
            Bid: Math.Round(basePrice - spread, 2),
            Ask: Math.Round(basePrice + spread, 2),
            Last: Math.Round(basePrice, 2),
            Volume: rnd.Next(1, 10000)); // Увеличил максимальный объем
    }
}

