// StrategyProcessorRaceTests.cs
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

        // 1. Публикуем котировки — стратегия должна отправить первый ордер
        await bus.PublishAsync(newQuotes);
        await WaitForOrderCountAsync(sentOrders, expectedCount: 1, timeout: TimeSpan.FromSeconds(2));
        Assert.Single(sentOrders);

        // 2. Повторная публикация без ответа — новый ордер не должен уйти
        await bus.PublishAsync(newQuotes);
        await Task.Delay(200); // даём возможность ошибочно отправить ордер
        Assert.Single(sentOrders);

        // 3. Имитируем исполнение первого ордера
        var filled = new OrderFilled(Guid.NewGuid(), "AAPL", 10m, 101m, "Buy");
        await bus.PublishAsync(filled);
        await Task.Delay(100); // ждём снятия блокировки

        // 4. Снова публикуем котировки — теперь блокировка снята, ордер должен отправиться
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

//// StrategyProcessorRaceTests.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Processors;
//using Trading.Storage;
//using Microsoft.Extensions.Logging.Abstractions;
//using Xunit;

//namespace Trading.Tests;

//public class StrategyProcessorRaceTests
//{
//    [Fact]
//    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
//    {
//        // Arrange
//        var bus = new InMemoryMicroEventBus();
//        var db = new InMemoryDatabase();
//        var loggerFactory = NullLoggerFactory.Instance;

//        var strategy = new StrategyProcessor(bus, loggerFactory, db);

//        int orderCount = 0;
//        var firstOrderTcs = new TaskCompletionSource<OrderRequested>(TaskCreationOptions.RunContinuationsAsynchronously);
//        var secondOrderTcs = new TaskCompletionSource<OrderRequested>(TaskCreationOptions.RunContinuationsAsynchronously);

//        bus.Subscribe<OrderRequested>((order, ct) =>
//        {
//            int current = Interlocked.Increment(ref orderCount);
//            if (current == 1)
//                firstOrderTcs.TrySetResult(order);
//            else if (current == 2)
//                secondOrderTcs.TrySetResult(order);
//            return Task.CompletedTask;
//        });

//        var quote = new Quote("AAPL", 100m, 101m, DateTime.UtcNow);
//        var newQuotes = new NewQuotes(new[] { quote });

//        // 1. Публикуем котировки — стратегия должна отправить первый ордер
//        await bus.PublishAsync(newQuotes);
//        var firstOrder = await firstOrderTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

//        // 2. Повторная публикация до получения ответа — новый ордер не должен уйти
//        await bus.PublishAsync(newQuotes);
//        await Task.Delay(200);
//        Assert.Equal(1, orderCount);

//        // 3. Имитируем исполнение первого ордера
//        var filled = new OrderFilled(Guid.NewGuid(), firstOrder.Ticker, firstOrder.Quantity, firstOrder.Price, firstOrder.Side);
//        await bus.PublishAsync(filled);

//        // 4. Снова публикуем котировки — теперь блокировка снята
//        await bus.PublishAsync(newQuotes);
//        var secondOrder = await secondOrderTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

//        Assert.Equal(2, orderCount);
//        Assert.Equal("AAPL", secondOrder.Ticker);
//    }
//}

//// StrategyProcessorRaceTests.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Processors;
//using Trading.Storage;
//using Microsoft.Extensions.Logging.Abstractions;
//using Xunit;

//namespace Trading.Tests;

//public class StrategyProcessorRaceTests
//{
//    [Fact]
//    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
//    {
//        // Arrange
//        var bus = new InMemoryMicroEventBus();
//        var db = new InMemoryDatabase();
//        var loggerFactory = NullLoggerFactory.Instance;

//        var strategy = new StrategyProcessor(bus, loggerFactory, db);

//        int orderCount = 0;
//        var firstOrderTcs = new TaskCompletionSource<OrderRequested>(TaskCreationOptions.RunContinuationsAsynchronously);
//        var secondOrderTcs = new TaskCompletionSource<OrderRequested>(TaskCreationOptions.RunContinuationsAsynchronously);

//        bus.Subscribe<OrderRequested>((order, ct) =>
//        {
//            int current = Interlocked.Increment(ref orderCount);
//            if (current == 1)
//                firstOrderTcs.TrySetResult(order);
//            else if (current == 2)
//                secondOrderTcs.TrySetResult(order);
//            return Task.CompletedTask;
//        });

//        var quote = new Quote("AAPL", 100m, 101m, DateTime.UtcNow);
//        var newQuotes = new NewQuotes(new[] { quote });

//        // 1. Публикуем котировки — стратегия должна отправить первый ордер
//        await bus.PublishAsync(newQuotes);
//        var firstOrder = await firstOrderTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

//        // 2. Публикуем котировки повторно до получения ответа — новый ордер не должен уйти
//        await bus.PublishAsync(newQuotes);
//        await Task.Delay(200); // небольшая пауза, чтобы дать возможность ошибочно отправить ордер
//        Assert.Equal(1, orderCount);

//        // 3. Имитируем исполнение первого ордера
//        var filled = new OrderFilled(firstOrder.OrderId, firstOrder.Ticker, firstOrder.Quantity, firstOrder.Price, firstOrder.Side);
//        await bus.PublishAsync(filled);

//        // 4. Снова публикуем котировки — теперь блокировка снята, должен уйти второй ордер
//        await bus.PublishAsync(newQuotes);
//        var secondOrder = await secondOrderTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

//        Assert.Equal(2, orderCount);
//        Assert.NotEqual(firstOrder.OrderId, secondOrder.OrderId);
//    }
//}

//// StrategyProcessorRaceTests.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Processors;
//using Trading.Storage;
//using Microsoft.Extensions.Logging.Abstractions;
//using Xunit;

//namespace Trading.Tests;

//public class StrategyProcessorRaceTests
//{
//    [Fact]
//    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
//    {
//        // Arrange
//        var bus = new InMemoryMicroEventBus();
//        var db = new InMemoryDatabase();
//        var loggerFactory = NullLoggerFactory.Instance;

//        var strategy = new StrategyProcessor(bus, loggerFactory, db);

//        var sentOrders = new List<OrderRequested>();
//        // Подписываемся на OrderRequested, чтобы отслеживать отправленные ордера
//        bus.Subscribe<OrderRequested>((order, ct) =>
//        {
//            lock (sentOrders) { sentOrders.Add(order); }
//            return Task.CompletedTask;
//        });

//        // Создаём котировку для AAPL
//        var quote = new Quote("AAPL", 100m, 101m, DateTime.UtcNow);
//        var newQuotes = new NewQuotes(new[] { quote });

//        // Первый вызов NewQuotes – стратегия должна отправить ордер на покупку
//        await bus.PublishAsync(newQuotes);
//        await Task.Delay(200); // даём время на обработку

//        Assert.Single(sentOrders);

//        // Второй вызов NewQuotes до получения ответа – новый ордер отправляться не должен
//        await bus.PublishAsync(newQuotes);
//        await Task.Delay(200);

//        Assert.Single(sentOrders);

//        // Имитируем ответ OrderFilled (ордер исполнен)
//        var orderId = Guid.NewGuid();
//        var filled = new OrderFilled(orderId, "AAPL", 10m, 101m, "Buy");
//        await bus.PublishAsync(filled);
//        await Task.Delay(200);

//        // Теперь снова вызываем NewQuotes – стратегия должна отправить новый ордер
//        await bus.PublishAsync(newQuotes);
//        await Task.Delay(200);

//        Assert.Equal(2, sentOrders.Count);
//    }
//}

//// StrategyProcessorRaceTests.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Processors;
//using Trading.Storage;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using Xunit;

//namespace Trading.Tests;

//public class StrategyProcessorRaceTests
//{
//    [Fact]
//    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
//    {
//        // Arrange
//        var busMock = new Mock<IMicroEventBus>();
//        var db = new InMemoryDatabase();
//        var loggerFactory = NullLoggerFactory.Instance;

//        // Захватываем делегаты подписок
//        Func<NewQuotes, CancellationToken, Task>? newQuotesHandler = null;
//        Func<OrderFilled, CancellationToken, Task>? filledHandler = null;
//        Func<OrderNotFilled, CancellationToken, Task>? notFilledHandler = null;

//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<NewQuotes, CancellationToken, Task>>()))
//               .Callback<Func<NewQuotes, CancellationToken, Task>>(h => newQuotesHandler = h);
//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<OrderFilled, CancellationToken, Task>>()))
//               .Callback<Func<OrderFilled, CancellationToken, Task>>(h => filledHandler = h);
//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<OrderNotFilled, CancellationToken, Task>>()))
//               .Callback<Func<OrderNotFilled, CancellationToken, Task>>(h => notFilledHandler = h);

//        var strategy = new StrategyProcessor(busMock.Object, loggerFactory, db);

//        // Убеждаемся, что подписки прошли
//        Assert.NotNull(newQuotesHandler);
//        Assert.NotNull(filledHandler);
//        Assert.NotNull(notFilledHandler);

//        // Создаём котировку (позиционные аргументы)
//        var quote = new Quote("AAPL", 100m, 101m, DateTime.UtcNow);
//        var newQuotes = new NewQuotes(new[] { quote });

//        // Первый вызов NewQuotes – стратегия должна отправить ордер на покупку
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        // Проверяем, что был вызван PublishAsync с OrderRequested один раз
//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Once);

//        // Второй вызов NewQuotes до получения ответа – новый ордер отправляться не должен
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Once,
//            "Должен быть только один ордер, пока предыдущий не разрешён");

//        // Имитируем ответ OrderFilled
//        var orderId = Guid.NewGuid();
//        var filled = new OrderFilled(orderId, "AAPL", 10m, 101m, "Buy"); // позиционные аргументы
//        await filledHandler!(filled, CancellationToken.None);

//        // Теперь снова вызываем NewQuotes – стратегия должна отправить новый ордер
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Exactly(2),
//            "После получения OrderFilled должен быть отправлен новый ордер");
//    }
//}


//// StrategyProcessorRaceTests.cs
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using System.Timers;
//using Trading.Core;
//using Trading.Domain;
//using Trading.Processors;
//using Trading.Storage;
//using Xunit;

//namespace Trading.Tests;

//public class StrategyProcessorRaceTests
//{
//    [Fact]
//    public async Task Strategy_Should_Not_Send_New_Order_Until_Previous_Is_Resolved()
//    {
//        // Arrange
//        var busMock = new Mock<IMicroEventBus>();
//        var db = new InMemoryDatabase();
//        var loggerFactory = NullLoggerFactory.Instance;

//        // Захватываем делегаты подписок
//        Func<NewQuotes, CancellationToken, Task>? newQuotesHandler = null;
//        Func<OrderFilled, CancellationToken, Task>? filledHandler = null;
//        Func<OrderNotFilled, CancellationToken, Task>? notFilledHandler = null;

//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<NewQuotes, CancellationToken, Task>>()))
//               .Callback<Func<NewQuotes, CancellationToken, Task>>(h => newQuotesHandler = h);
//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<OrderFilled, CancellationToken, Task>>()))
//               .Callback<Func<OrderFilled, CancellationToken, Task>>(h => filledHandler = h);
//        busMock.Setup(b => b.Subscribe(It.IsAny<Func<OrderNotFilled, CancellationToken, Task>>()))
//               .Callback<Func<OrderNotFilled, CancellationToken, Task>>(h => notFilledHandler = h);

//        var strategy = new StrategyProcessor(busMock.Object, loggerFactory, db);

//        // Убеждаемся, что подписки прошли
//        Assert.NotNull(newQuotesHandler);
//        Assert.NotNull(filledHandler);
//        Assert.NotNull(notFilledHandler);

//        // Создаём котировку
//        var quote = new Quote("AAPL", bid: 100m, ask: 101m, DateTime.UtcNow);
//        var newQuotes = new NewQuotes(new[] { quote });

//        // Первый вызов NewQuotes – стратегия должна отправить ордер на покупку
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        // Проверяем, что был вызван PublishAsync с OrderRequested один раз
//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Once);

//        // Второй вызов NewQuotes до получения ответа – новый ордер отправляться не должен
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Once,
//            "Должен быть только один ордер, пока предыдущий не разрешён");

//        // Имитируем ответ OrderFilled
//        var orderId = Guid.NewGuid();
//        var filled = new OrderFilled(orderId, "AAPL", quantity: 10, price: 101m, Side: "Buy");
//        await filledHandler!(filled, CancellationToken.None);

//        // Теперь снова вызываем NewQuotes – стратегия должна отправить новый ордер
//        await newQuotesHandler!(newQuotes, CancellationToken.None);

//        busMock.Verify(b => b.PublishAsync(It.IsAny<OrderRequested>(), It.IsAny<CancellationToken>()), Times.Exactly(2),
//            "После получения OrderFilled должен быть отправлен новый ордер");
//    }
//}
