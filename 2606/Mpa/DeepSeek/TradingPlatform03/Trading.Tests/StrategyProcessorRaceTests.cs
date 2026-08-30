// 26.08.30

using System.Diagnostics;
using Trading.Core;
using Trading.Domain;
using Trading.Processors;
using Trading.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Trading.Tests;

public class StrategyProcessorRaceTests
{
    [Fact]
    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
    {
        // Arrange
        var bus = new InMemoryMicroEventBus();
        var db = new InMemoryDatabase();
        var loggerFactory = NullLoggerFactory.Instance;

        var strategy = new StrategyProcessor(bus, loggerFactory, db);

        var sentOrders = new List<OrderRequested>();
        var lockObj = new object();
        bus.Subscribe<OrderRequested>((order, ct) =>
        {
            lock (lockObj) sentOrders.Add(order);
            return Task.CompletedTask;
        });

        var quote = new Quote("AAPL", 100m, 101m, DateTime.UtcNow);
        var newQuotes = new NewQuotes(new[] { quote });

        await bus.PublishAsync(newQuotes);
        await WaitForOrderCountAsync(sentOrders, expectedCount: 1, timeout: TimeSpan.FromSeconds(2));
        Assert.Single(sentOrders);

        await bus.PublishAsync(newQuotes);
        await Task.Delay(200);
        Assert.Single(sentOrders);

        var filled = new OrderFilled(Guid.NewGuid(), "AAPL", 10m, 101m, "Buy");
        await bus.PublishAsync(filled);
        await Task.Delay(100);

        await bus.PublishAsync(newQuotes);
        await WaitForOrderCountAsync(sentOrders, expectedCount: 2, timeout: TimeSpan.FromSeconds(2));
        Assert.Equal(2, sentOrders.Count);
    }

    private static async Task WaitForOrderCountAsync(List<OrderRequested> orders, int expectedCount, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            lock (orders)
            {
                if (orders.Count >= expectedCount)
                    return;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Expected {expectedCount} orders but got {orders.Count} within {timeout}");
    }
}