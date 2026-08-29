// InMemoryDatabase.cs
using System.Collections.Concurrent;
using System.Diagnostics;
using Trading.Domain;

namespace Trading.Storage;

public class InMemoryDatabase
{
    public ConcurrentDictionary<Guid, OrderCreated> Orders { get; } = new();
    public ConcurrentDictionary<Guid, Trade> Trades { get; } = new();
    public ConcurrentDictionary<string, PositionUpdated> Positions { get; } = new();

    public void SaveOrder(OrderCreated order) => Orders[order.OrderId] = order;
    public void SaveTrade(Trade trade) => Trades[trade.TradeId] = trade;
    public void UpdatePosition(PositionUpdated position) => Positions[position.Ticker] = position;
}
