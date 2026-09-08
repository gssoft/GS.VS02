using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusLibrary;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubStaticModeTests
{
    private ServiceProvider _provider;
    private IEventHub _hub;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddStaticEventHub(opts =>
        {
            opts.ChannelCapacity = 100;
            opts.Assemblies = new[] { typeof(StaticTestHandler).Assembly };
        });

        _provider = services.BuildServiceProvider();
        _hub = _provider.GetRequiredService<IEventHub>();

        StaticTestHandler.ReceivedMessages.Clear();
        AnotherStaticHandler.ReceivedMessages.Clear();

        // Дадим время на запуск фонового потока
        await Task.Delay(100);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_hub is IAsyncDisposable ad)
            await ad.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [TestMethod]
    public async Task StaticHandler_ReceivesMessage()
    {
        var msg = new TestMessage();
        await _hub.PublishAsync(msg);

        // Дадим время на обработку
        await Task.Delay(300);

        Assert.HasCount(1, StaticTestHandler.ReceivedMessages);
        Assert.AreEqual("test:message", StaticTestHandler.ReceivedMessages.First().Key);
    }

    [TestMethod]
    public async Task DynamicSubscription_IsIgnored_InStaticMode()
    {
        var dynamicManager = _provider.GetRequiredService<IDynamicSubscriptionManager>();
        var dynamicHandler = new DynamicTestHandler();

        dynamicManager.Subscribe<TestMessage>("test:message", (msg, ct) =>
        {
            dynamicHandler.Received.Add(msg);
            return ValueTask.CompletedTask;
        });

        var msg = new TestMessage();
        await _hub.PublishAsync(msg);
        await Task.Delay(300);

        Assert.HasCount(1, StaticTestHandler.ReceivedMessages);
        Assert.IsEmpty(dynamicHandler.Received, "Dynamic handler should not be called in static mode");
    }

    [TestMethod]
    public async Task MessageWithoutHandler_DoesNotThrow()
    {
        var msg = new AnotherTestMessage(); // Ключ "another:message", но AnotherStaticHandler не должен мешать
        await _hub.PublishAsync(msg);
        await Task.Delay(200);

        // Сообщение должно быть просто проигнорировано без ошибок
    }
}

