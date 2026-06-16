// Providers/MarketDataProvider.cs

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TradingTerminal.Models;

namespace TradingTerminal.Providers;

public static class MarketDataProvider
{
    // Метод теперь принимает CancellationToken для корректной остановки
    public static async IAsyncEnumerable<object> GetMarketDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rnd = new Random();
        while (!cancellationToken.IsCancellationRequested)
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

            await Task.Delay(200, cancellationToken); // Пауза между тиками, передаем токен
        }
    }
}

/*
 * MarketDataProvider теперь не является генератором данных, а лишь содержит логику их создания.
 * Сам процесс генерации переносится в фоновый сервис.
 */