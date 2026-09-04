using System.Collections.Concurrent;
using Trading.Core;
using Trading.Domain;
using Trading.EventBus.RabbitMQ;

namespace Trading.Dashboard.Services;

public class EventMonitorService : IAsyncDisposable
{
    private readonly RabbitMqMicroEventBus _bus;
    private readonly ConcurrentQueue<EventRecord> _events = new();
    private const int MaxEvents = 100;

    public EventMonitorService(IConfiguration configuration)
    {
        _bus = new RabbitMqMicroEventBus(
            configuration["EventBus:RabbitMQ:HostName"] ?? "localhost",
            configuration.GetValue<int>("EventBus:RabbitMQ:Port", 5672),
            configuration["EventBus:RabbitMQ:Username"] ?? "guest",
            configuration["EventBus:RabbitMQ:Password"] ?? "guest");
        SubscribeToEvents();
    }

   

    private void SubscribeToEvents()
    {
        _bus.Subscribe<OrderCreated>((e, ct) => AddEvent(nameof(OrderCreated), e));
        _bus.Subscribe<OrderFilled>((e, ct) => AddEvent(nameof(OrderFilled), e));
        _bus.Subscribe<OrderNotFilled>((e, ct) => AddEvent(nameof(OrderNotFilled), e));
        _bus.Subscribe<Trade>((e, ct) => AddEvent(nameof(Trade), e));
        _bus.Subscribe<PositionUpdated>((e, ct) => AddEvent(nameof(PositionUpdated), e));
        _bus.Subscribe<PortfolioSnapshot>((e, ct) => AddEvent(nameof(PortfolioSnapshot), e));
    }

    private Task AddEvent<T>(string type, T evt) where T : class
    {
        _events.Enqueue(new EventRecord { EventType = type, Data = evt });
        while (_events.Count > MaxEvents && _events.TryDequeue(out _)) { }
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<EventRecord> GetRecentEvents() => _events.Reverse().ToList();

    public async ValueTask DisposeAsync()
    {
        _bus.Dispose();
        await Task.CompletedTask;
    }
}

public class EventRecord
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}