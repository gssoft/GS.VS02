// PositionProcessor.cs
using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class PositionProcessor : EventDrivenProcessor<Trade>
{
    private readonly InMemoryDatabase _db;

    public PositionProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
        : base(bus, loggerFactory)
    {
        _db = db;
    }

    protected override async Task HandleAsync(Trade trade, CancellationToken ct)
    {
        _db.Positions.AddOrUpdate(trade.Ticker,
            ticker => new PositionUpdated
            {
                Ticker = ticker,
                Quantity = trade.Side == "Buy" ? trade.Quantity : -trade.Quantity,
                AveragePrice = trade.Price
            },
            (ticker, existing) =>
            {
                decimal newQty = existing.Quantity + (trade.Side == "Buy" ? trade.Quantity : -trade.Quantity);
                decimal avgPrice;
                if (newQty == 0)
                {
                    avgPrice = 0; // позиция закрыта
                }
                else if (trade.Side == "Buy")
                {
                    avgPrice = (existing.AveragePrice * existing.Quantity + trade.Price * trade.Quantity) / newQty;
                }
                else
                {
                    avgPrice = existing.AveragePrice;
                }
                return new PositionUpdated { Ticker = ticker, Quantity = newQty, AveragePrice = avgPrice };
            });

        var updated = _db.Positions[trade.Ticker];
        await Bus.PublishAsync(updated, ct);
        Logger.LogInformation("Position updated for {Ticker}: {Quantity}", trade.Ticker, updated.Quantity);
    }

    //protected override async Task HandleAsync(Trade trade, CancellationToken ct)
    //{
    //    _db.Positions.AddOrUpdate(trade.Ticker,
    //        ticker => new PositionUpdated { Ticker = ticker, Quantity = trade.Side == "Buy" ? trade.Quantity : -trade.Quantity, AveragePrice = trade.Price },
    //        (ticker, existing) =>
    //        {
    //            decimal newQty = existing.Quantity + (trade.Side == "Buy" ? trade.Quantity : -trade.Quantity);
    //            decimal avgPrice;
    //            if (newQty == 0)
    //            {
    //                avgPrice = 0; // позиция закрыта
    //            }
    //            else if (trade.Side == "Buy")
    //            {
    //                avgPrice = (existing.AveragePrice * existing.Quantity + trade.Price * trade.Quantity) / newQty;
    //            }
    //            else
    //            {
    //                avgPrice = existing.AveragePrice;
    //            }
    //            return new PositionUpdated { Ticker = ticker, Quantity = newQty, AveragePrice = avgPrice };
    //        });

    //    var updated = _db.Positions[trade.Ticker];
    //    await Bus.PublishAsync(updated, ct);
    //    Logger.LogInformation("Position updated for {Ticker}: {Quantity}", trade.Ticker, updated.Quantity);
    //}

    //protected override async Task HandleAsync(Trade trade, CancellationToken ct)
    //{
    //    _db.Positions.AddOrUpdate(trade.Ticker,
    //        ticker => // new PositionUpdated(ticker, trade.Side == "Buy" ? trade.Quantity : -trade.Quantity, trade.Price),
    //        new PositionUpdated { Ticker = ticker, Quantity = trade.Side == "Buy" ? trade.Quantity : -trade.Quantity, AveragePrice = trade.Price },
    //        (ticker, existing) =>
    //        {
    //            decimal newQty = existing.Quantity + (trade.Side == "Buy" ? trade.Quantity : -trade.Quantity);
    //            decimal avgPrice = (existing.AveragePrice * existing.Quantity + trade.Price * trade.Quantity) / (existing.Quantity + trade.Quantity);
    //            return // new PositionUpdated(ticker, newQty, avgPrice);
    //                    new PositionUpdated { Ticker = ticker, Quantity = newQty, AveragePrice = avgPrice };
    //        });

    //    var updated = _db.Positions[trade.Ticker];
    //    await Bus.PublishAsync(updated, ct);
    //    Logger.LogInformation("Position updated for {Ticker}: {Quantity}", trade.Ticker, updated.Quantity);
    //}
}
