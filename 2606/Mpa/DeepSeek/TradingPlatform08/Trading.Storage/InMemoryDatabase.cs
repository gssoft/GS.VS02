//// InMemoryDatabase.cs

using System.Collections.Concurrent;
using Trading.Domain;

namespace Trading.Storage;

public class InMemoryDatabase
{
    private const int MaxEventLogSize = 42;

    public ConcurrentDictionary<Guid, OrderCreated> Orders { get; } = new();
    public ConcurrentDictionary<Guid, Trade> Trades { get; } = new();
    public ConcurrentDictionary<string, PositionUpdated> Positions { get; } = new();

    // Храним не просто объект, а пару "тип события + данные"
    public ConcurrentQueue<EventLogEntry> EventLog { get; } = new();

    public void SaveOrder(OrderCreated order) => Orders[order.OrderId] = order;
    public void SaveTrade(Trade trade) => Trades[trade.TradeId] = trade;
    public void UpdatePosition(PositionUpdated position) => Positions[position.Ticker] = position;

    //public void LogEvent(string eventType, object evt)
    //{
    //    EventLog.Enqueue(new EventLogEntry { EventType = eventType, Data = evt });
    //}

    public void LogEvent(string eventType, object evt)
    {
        EventLog.Enqueue(new EventLogEntry { EventType = eventType, Data = evt });
        while (EventLog.Count > MaxEventLogSize && EventLog.TryDequeue(out _)) { }
    }
}

public class EventLogEntry
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

//// InMemoryDatabase.cs
//using System.Collections.Concurrent;
//using Trading.Domain;

//namespace Trading.Storage;

//public class InMemoryDatabase
//{
//    public ConcurrentDictionary<Guid, OrderCreated> Orders { get; } = new();
//    public ConcurrentDictionary<Guid, Trade> Trades { get; } = new();
//    public ConcurrentDictionary<string, PositionUpdated> Positions { get; } = new();

//    // Новая коллекция для хранения последних событий (для UI)
//    public ConcurrentQueue<object> EventLog { get; } = new();

//    public void SaveOrder(OrderCreated order) => Orders[order.OrderId] = order;
//    public void SaveTrade(Trade trade) => Trades[trade.TradeId] = trade;
//    public void UpdatePosition(PositionUpdated position) => Positions[position.Ticker] = position;

//    // Метод для логирования событий
//    public void LogEvent(object evt) => EventLog.Enqueue(evt);
//}
