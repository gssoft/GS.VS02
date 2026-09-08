using BusLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubBothModeTests
{
    private ServiceProvider _provider = null!;
    private IEventHub _hub = null!;
    private IDynamicSubscriptionManager _dynamicManager = null!;
    private StaticTestHandler _staticHandler = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddEventHub(opts =>
        {
            opts.ChannelCapacity = 100;
            opts.Assemblies = new[] { typeof(StaticTestHandler).Assembly };
        });

        _provider = services.BuildServiceProvider();
        _hub = _provider.GetRequiredService<IEventHub>();
        _dynamicManager = _provider.GetRequiredService<IDynamicSubscriptionManager>();
        _staticHandler = _provider.GetRequiredService<StaticTestHandler>();

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

        Assert.AreEqual(1, _staticHandler.ReceivedMessages.Count);
        Assert.AreEqual(1, dynamicHandler.Received.Count);
    }
}
