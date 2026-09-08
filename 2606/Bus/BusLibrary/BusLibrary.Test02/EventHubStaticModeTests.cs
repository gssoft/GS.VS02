using BusLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubStaticModeTests
{
    private ServiceProvider _provider = null!;
    private IEventHub _hub = null!;
    private StaticTestHandler _staticHandler = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddStaticEventHub(opts =>
        {
            opts.ChannelCapacity = 100;
            // Явно указываем сборку с обработчиками
            opts.Assemblies = new[] { typeof(StaticTestHandler).Assembly };
        });

        _provider = services.BuildServiceProvider();
        _hub = _provider.GetRequiredService<IEventHub>();
        _staticHandler = _provider.GetRequiredService<StaticTestHandler>();

        // Ждём запуска pump
        await Task.Delay(100);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_hub is IAsyncDisposable ad) await ad.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [TestMethod]
    public async Task StaticHandler_ReceivesMessage()
    {
        var msg = new TestMessage();
        await _hub.PublishAsync(msg);
        await Task.Delay(300);

        Assert.AreEqual(1, _staticHandler.ReceivedMessages.Count);
        Assert.AreEqual("test:message", _staticHandler.ReceivedMessages.First().Key);
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

        Assert.AreEqual(1, _staticHandler.ReceivedMessages.Count);
        Assert.AreEqual(0, dynamicHandler.Received.Count,
            "Dynamic handler should not be called in static mode");
    }

    [TestMethod]
    public async Task MessageWithoutHandler_DoesNotThrow()
    {
        var msg = new AnotherTestMessage();
        // Не должно быть исключений, даже если AnotherStaticHandler существует
        await _hub.PublishAsync(msg);
        await Task.Delay(200);
    }
}

