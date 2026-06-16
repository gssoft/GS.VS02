// Providers/MarketDataProvider.cs

using System.Collections.Generic;
using TradingTerminal.Models;

namespace TradingTerminal.Providers;

public static class MarketDataProvider
{
    public static async IAsyncEnumerable<object> GetMarketDataAsync()
    {
        var rnd = new Random();
        while (true)
        {
            var quote = new Quote
            {
                Price = 150m + (decimal)rnd.NextDouble() * 5m // Цена от 150 до 155
            };
            yield return quote;

            // 10% шанс сгенерировать сделку по текущей цене
            if (rnd.Next(10) == 0)
            {
                yield return new Trade { Price = quote.Price, Volume = rnd.Next(10, 100) };
            }

            await Task.Delay(200); // Пауза между тиками
        }
    }
}