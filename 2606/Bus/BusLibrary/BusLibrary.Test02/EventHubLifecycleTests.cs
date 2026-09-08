using BusLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Channels;

namespace BusLibrary.Tests;

[TestClass]
public class EventHubLifecycleTests
{
    [TestMethod]
    public async Task DisposeAsync_CompletesWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddEventHub();

        var provider = services.BuildServiceProvider();
        var hub = provider.GetRequiredService<IEventHub>();

        for (int i = 0; i < 10; i++)
            await hub.PublishAsync(new TestMessage());

        await Task.Delay(200);

        if (hub is IAsyncDisposable ad) await ad.DisposeAsync();
        await provider.DisposeAsync();
    }

    [TestMethod]
    public async Task PublishingAfterDispose_CompletesSilently()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddEventHub();

        var provider = services.BuildServiceProvider();
        var hub = provider.GetRequiredService<IEventHub>();

        if (hub is IAsyncDisposable ad) await ad.DisposeAsync();

        // После Dispose публикация не должна кидать исключений (ChannelClosedException глушится)
        await hub.PublishAsync(new TestMessage());
    }
}
