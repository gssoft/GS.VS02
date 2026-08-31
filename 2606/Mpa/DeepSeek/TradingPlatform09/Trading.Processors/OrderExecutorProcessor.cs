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
        var order = //new OrderCreated(Guid.NewGuid(), request.Ticker, request.Quantity, request.Price, request.Side, DateTime.UtcNow);
            new OrderCreated { OrderId = Guid.NewGuid(), Ticker = request.Ticker, Quantity = request.Quantity, Price = request.Price, Side = request.Side, Timestamp = DateTime.UtcNow };

        _db.SaveOrder(order);
        await Bus.PublishAsync(order, ct); // публикуем OrderCreated (не обязательно)

        // Имитация исполнения: 70% - filled, 30% - not filled
        if (_rnd.NextDouble() < _fillProbability)
        {
            var filled = // OrderFilled(order.OrderId, order.Ticker, order.Quantity, order.Price, order.Side);
                new OrderFilled { OrderId = order.OrderId, Ticker = order.Ticker, Quantity = order.Quantity, Price = order.Price, Side = order.Side };
            await Bus.PublishAsync(filled, ct);
            Logger.LogInformation("Order {OrderId} FILLED", order.OrderId);
        }
        else
        {
            // var notFilled = new OrderNotFilled(order.OrderId, "Random rejection");

            var notFilled = // new OrderNotFilled(order.OrderId, order.Ticker, "Random rejection");
                new OrderNotFilled { OrderId = order.OrderId, Ticker = order.Ticker, Reason = "Random rejection" };
            await Bus.PublishAsync(notFilled, ct);
            Logger.LogInformation("Order {OrderId} NOT FILLED", order.OrderId);
        }
    }
}
