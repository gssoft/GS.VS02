using BusLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubDynamicModeTests
{
    private ServiceProvider _provider = null!;
    private IEventHub _hub = null!;
    private IDynamicSubscriptionManager _dynamicManager = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddDynamicEventHub(opts => opts.ChannelCapacity = 100);

        _provider = services.BuildServiceProvider();
        _hub = _provider.GetRequiredService<IEventHub>();
        _dynamicManager = _provider.GetRequiredService<IDynamicSubscriptionManager>();

        await Task.Delay(100);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_hub is IAsyncDisposable ad) await ad.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [TestMethod]
    public async Task DynamicHandler_ReceivesMessage()
    {
        var handler = new DynamicTestHandler();
        _dynamicManager.Subscribe<TestMessage>("test:message", (msg, ct) =>
        {
            handler.Received.Add(msg);
            return ValueTask.CompletedTask;
        });

        var msg = new TestMessage();
        await _hub.PublishAsync(msg);
        await Task.Delay(300);

        Assert.AreEqual(1, handler.Received.Count);
        Assert.AreEqual("test:message", handler.Received.First().Key);
    }

    [TestMethod]
    public async Task StaticHandler_IsIgnored_InDynamicMode()
    {
        var handler = new DynamicTestHandler();
        _dynamicManager.Subscribe<TestMessage>("test:message", (msg, ct) =>
        {
            handler.Received.Add(msg);
            return ValueTask.CompletedTask;
        });

        var msg = new TestMessage();
        await _hub.PublishAsync(msg);
        await Task.Delay(300);

        // В dynamic mode статические обработчики вообще не должны загружаться
        // Проверим, что никакой StaticTestHandler не вызывался
        // (он даже не зарегистрирован в DI, т.к. сборки не передавали)
        // Если он случайно зарегистрирован, тест упадет
        Assert.IsFalse(_provider.GetServices<StaticTestHandler>().Any(),
            "Static handler should not be registered in dynamic mode");
        Assert.AreEqual(1, handler.Received.Count);
    }

    [TestMethod]
    public async Task Unsubscribe_StopsDelivery()
    {
        var handler = new DynamicTestHandler();
        var subscription = _dynamicManager.Subscribe<TestMessage>("test:message", (msg, ct) =>
        {
            handler.Received.Add(msg);
            return ValueTask.CompletedTask;
        });

        await _hub.PublishAsync(new TestMessage());
        await Task.Delay(200);
        Assert.AreEqual(1, handler.Received.Count);

        subscription.Dispose();

        await _hub.PublishAsync(new TestMessage());
        await Task.Delay(200);
        Assert.AreEqual(1, handler.Received.Count, "No more messages after unsubscribe");
    }
}
