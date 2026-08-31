using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.Core;
using Trading.Domain;
using Trading.Storage;

namespace Trading.Processors;

public class EventLogProcessor : IHostedService, IDisposable
{
    private readonly IMicroEventBus _bus;
    private readonly InMemoryDatabase _db;
    private readonly ILogger<EventLogProcessor> _logger;

    public EventLogProcessor(IMicroEventBus bus, InMemoryDatabase db, ILogger<EventLogProcessor> logger)
    {
        _bus = bus;
        _db = db;
        _logger = logger;

        // Подписки на события
        _bus.Subscribe<OrderCreated>(OnOrderCreated);
        _bus.Subscribe<OrderFilled>(OnOrderFilled);
        _bus.Subscribe<OrderNotFilled>(OnOrderNotFilled);
        _bus.Subscribe<Trade>(OnTrade);
        _bus.Subscribe<PositionUpdated>(OnPositionUpdated);
        _bus.Subscribe<PortfolioSnapshot>(OnPortfolioSnapshot);
    }

    private Task OnOrderCreated(OrderCreated evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }
    private Task OnOrderFilled(OrderFilled evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }
    private Task OnOrderNotFilled(OrderNotFilled evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }
    private Task OnTrade(Trade evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }
    private Task OnPositionUpdated(PositionUpdated evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }
    private Task OnPortfolioSnapshot(PortfolioSnapshot evt, CancellationToken ct)
    {
        _db.LogEvent(evt);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() { }
}
