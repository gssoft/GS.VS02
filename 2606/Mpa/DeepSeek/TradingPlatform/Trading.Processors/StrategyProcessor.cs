// StrategyProcessor.cs
using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class StrategyProcessor : EventDrivenProcessor<NewQuotes>
{
    private readonly InMemoryDatabase _db;
    private readonly Random _rnd = new();

    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
        : base(bus, loggerFactory)
    {
        _db = db;
    }

    protected override async Task HandleAsync(NewQuotes message, CancellationToken ct)
    {
        foreach (var quote in message.Quotes)
        {
            // Простая стратегия: если нет позиции – покупаем, если есть – продаём (для примера)
            bool hasPosition = _db.Positions.TryGetValue(quote.Ticker, out var pos) && pos.Quantity != 0;

            if (!hasPosition)
            {
                // Открываем позицию
                var order = new OrderRequested(quote.Ticker, quantity: 10, price: quote.Ask, side: "Buy");
                await Bus.PublishAsync(order, ct);
                Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", quote.Ticker, quote.Ask);
            }
            else
            {
                // Закрываем позицию
                var order = new OrderRequested(quote.Ticker, quantity: pos.Quantity, price: quote.Bid, side: "Sell");
                await Bus.PublishAsync(order, ct);
                Logger.LogInformation("Strategy: Sending SELL order for {Ticker} at {Price}", quote.Ticker, quote.Bid);
            }
        }
    }
}
