// OrderExecutorProcessor.cs
using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class OrderExecutorProcessor : EventDrivenProcessor<OrderRequested>
{
    private readonly InMemoryDatabase _db;
    private readonly double _fillProbability;
    private readonly Random _rnd = new();

    public OrderExecutorProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db, double fillProbability = 0.7)
        : base(bus, loggerFactory)
    {
        _db = db;
        _fillProbability = fillProbability;
    }

    protected override async Task HandleAsync(OrderRequested request, CancellationToken ct)
    {
        var order = new OrderCreated(Guid.NewGuid(), request.Ticker, request.Quantity, request.Price, request.Side, DateTime.UtcNow);
        _db.SaveOrder(order);
        await Bus.PublishAsync(order, ct); // публикуем OrderCreated (не обязательно)

        // Имитация исполнения: 70% - filled, 30% - not filled
        if (_rnd.NextDouble() < _fillProbability)
        {
            var filled = new OrderFilled(order.OrderId, order.Ticker, order.Quantity, order.Price, order.Side);
            await Bus.PublishAsync(filled, ct);
            Logger.LogInformation("Order {OrderId} FILLED", order.OrderId);
        }
        else
        {
            // var notFilled = new OrderNotFilled(order.OrderId, "Random rejection");

            var notFilled = new OrderNotFilled(order.OrderId, order.Ticker, "Random rejection");
            await Bus.PublishAsync(notFilled, ct);
            Logger.LogInformation("Order {OrderId} NOT FILLED", order.OrderId);
        }
    }
}
