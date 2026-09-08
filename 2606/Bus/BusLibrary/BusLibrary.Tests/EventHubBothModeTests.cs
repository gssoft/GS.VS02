using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BusLibrary;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubBothModeTests
{
    private ServiceProvider _provider;
    private IEventHub _hub;
    private IDynamicSubscriptionManager _dynamicManager;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddEventHub(opts => opts.ChannelCapacity = 100); // default Both

        _provider = services.BuildServiceProvider();
        _hub = _provider.GetRequiredService<IEventHub>();
        _dynamicManager = _provider.GetRequiredService<IDynamicSubscriptionManager>();

        StaticTestHandler.ReceivedMessages.Clear();
        AnotherStaticHandler.ReceivedMessages.Clear();

        await Task.Delay(100);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_hub is IAsyncDisposable ad) await ad.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [TestMethod]
    public async Task BothStaticAndDynamic_ReceiveSameMessage()
    {
        var dynamicHandler = new DynamicTestHandler();
        _dynamicManager.Subscribe<TestMessage>("test:message", (msg, ct) =>
        {
            dynamicHandler.Received.Add(msg);
            return ValueTask.CompletedTask;
        });

        var msg = new TestMessage();
        await _hub.PublishAsync(msg);
        await Task.Delay(300);

        Assert.HasCount(1, StaticTestHandler.ReceivedMessages);
        Assert.HasCount(1, dynamicHandler.Received);
    }
}
