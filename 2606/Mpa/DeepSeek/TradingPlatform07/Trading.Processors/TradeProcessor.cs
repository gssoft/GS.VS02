// TradeProcessor.cs
using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class TradeProcessor : EventDrivenProcessor<OrderFilled>
{
    private readonly InMemoryDatabase _db;

    public TradeProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
        : base(bus, loggerFactory)
    {
        _db = db;
    }

    protected override async Task HandleAsync(OrderFilled filled, CancellationToken ct)
    {
        var trade = // new Trade(Guid.NewGuid(), filled.OrderId, filled.Ticker, filled.Quantity, filled.Price, filled.Side, DateTime.UtcNow);
            new Trade { TradeId = Guid.NewGuid(), OrderId = filled.OrderId, Ticker = filled.Ticker, Quantity = filled.Quantity, Price = filled.Price, Side = filled.Side, Timestamp = DateTime.UtcNow };
        _db.SaveTrade(trade);
        await Bus.PublishAsync(trade, ct);
        Logger.LogInformation("Trade created for {Ticker}", filled.Ticker);
    }
}
