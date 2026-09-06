using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Trading.Core;
using Trading.Domain;
using Trading.EventBus.RabbitMQ;
using Trading.Dashboard.Hubs;

namespace Trading.Dashboard.Services;

public class EventMonitorService : IHostedService, IDisposable
{
    private readonly RabbitMqMicroEventBus _bus;
    private readonly IHubContext<TradingHub> _hub;
    private readonly ILogger<EventMonitorService> _logger;
    private readonly ConcurrentQueue<EventRecord> _events = new();
    private const int MaxEvents = 100;

    public EventMonitorService(
        IConfiguration configuration,
        IHubContext<TradingHub> hub,
        ILogger<EventMonitorService> logger)
    {
        _hub = hub;
        _logger = logger;
        _bus = new RabbitMqMicroEventBus(
            configuration["EventBus:RabbitMQ:HostName"] ?? "localhost",
            configuration.GetValue<int>("EventBus:RabbitMQ:Port", 5672),
            configuration["EventBus:RabbitMQ:Username"] ?? "guest",
            configuration["EventBus:RabbitMQ:Password"] ?? "guest");
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _bus.Subscribe<NewQuotes>(async (e, ct) => await PublishEvent(nameof(NewQuotes), e));
        _bus.Subscribe<OrderCreated>(async (e, ct) => await PublishEvent(nameof(OrderCreated), e));
        _bus.Subscribe<OrderFilled>(async (e, ct) => await PublishEvent(nameof(OrderFilled), e));
        _bus.Subscribe<OrderNotFilled>(async (e, ct) => await PublishEvent(nameof(OrderNotFilled), e));
        _bus.Subscribe<Trade>(async (e, ct) => await PublishEvent(nameof(Trade), e));
        _bus.Subscribe<PositionUpdated>(async (e, ct) => await PublishEvent(nameof(PositionUpdated), e));
        _bus.Subscribe<PortfolioSnapshot>(async (e, ct) => await PublishEvent(nameof(PortfolioSnapshot), e));
    }

    private async Task PublishEvent(string eventType, object data)
    {
        // Локальная очередь для отладки
        _events.Enqueue(new EventRecord { EventType = eventType, Data = data });
        while (_events.Count > MaxEvents && _events.TryDequeue(out _)) { }

        // Рассылка через SignalR с типизированными методами
        switch (eventType)
        {
            case nameof(NewQuotes):
                await _hub.Clients.All.SendAsync("QuotesUpdated", (NewQuotes)data);
                break;
            case nameof(OrderCreated):
                await _hub.Clients.All.SendAsync("OrderCreated", (OrderCreated)data);
                break;
            case nameof(OrderFilled):
                await _hub.Clients.All.SendAsync("OrderFilled", (OrderFilled)data);
                break;
            case nameof(OrderNotFilled):
                await _hub.Clients.All.SendAsync("OrderNotFilled", (OrderNotFilled)data);
                break;
            case nameof(Trade):
                await _hub.Clients.All.SendAsync("TradeExecuted", (Trade)data);
                break;
            case nameof(PositionUpdated):
                await _hub.Clients.All.SendAsync("PositionUpdated", (PositionUpdated)data);
                break;
            case nameof(PortfolioSnapshot):
                await _hub.Clients.All.SendAsync("PnLUpdated", (PortfolioSnapshot)data);
                break;
        }
    }

    public IReadOnlyCollection<EventRecord> GetRecentEvents() => _events.Reverse().ToList();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventMonitorService started and subscribed to RabbitMQ.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bus.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _bus.Dispose();
    }
}

public class EventRecord
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

//using System.Collections.Concurrent;
//using Trading.Core;
//using Trading.Domain;
//using Trading.EventBus.RabbitMQ;

//namespace Trading.Dashboard.Services;

//public class EventMonitorService : IAsyncDisposable
//{
//    private readonly RabbitMqMicroEventBus _bus;
//    private readonly ConcurrentQueue<EventRecord> _events = new();
//    private const int MaxEvents = 100;

//    public EventMonitorService(IConfiguration configuration)
//    {
//        _bus = new RabbitMqMicroEventBus(
//            configuration["EventBus:RabbitMQ:HostName"] ?? "localhost",
//            configuration.GetValue<int>("EventBus:RabbitMQ:Port", 5672),
//            configuration["EventBus:RabbitMQ:Username"] ?? "guest",
//            configuration["EventBus:RabbitMQ:Password"] ?? "guest");
//        SubscribeToEvents();
//    }



//    private void SubscribeToEvents()
//    {
//        _bus.Subscribe<OrderCreated>((e, ct) => AddEvent(nameof(OrderCreated), e));
//        _bus.Subscribe<OrderFilled>((e, ct) => AddEvent(nameof(OrderFilled), e));
//        _bus.Subscribe<OrderNotFilled>((e, ct) => AddEvent(nameof(OrderNotFilled), e));
//        _bus.Subscribe<Trade>((e, ct) => AddEvent(nameof(Trade), e));
//        _bus.Subscribe<PositionUpdated>((e, ct) => AddEvent(nameof(PositionUpdated), e));
//        _bus.Subscribe<PortfolioSnapshot>((e, ct) => AddEvent(nameof(PortfolioSnapshot), e));
//    }

//    private Task AddEvent<T>(string type, T evt) where T : class
//    {
//        _events.Enqueue(new EventRecord { EventType = type, Data = evt });
//        while (_events.Count > MaxEvents && _events.TryDequeue(out _)) { }
//        return Task.CompletedTask;
//    }

//    public IReadOnlyCollection<EventRecord> GetRecentEvents() => _events.Reverse().ToList();

//    public async ValueTask DisposeAsync()
//    {
//        _bus.Dispose();
//        await Task.CompletedTask;
//    }
//}

//public class EventRecord
//{
//    public string EventType { get; set; } = string.Empty;
//    public object Data { get; set; } = new();
//}