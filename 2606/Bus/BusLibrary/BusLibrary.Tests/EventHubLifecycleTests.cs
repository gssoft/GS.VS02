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

        // Публикация нескольких сообщений
        for (int i = 0; i < 10; i++)
            await hub.PublishAsync(new TestMessage());

        await Task.Delay(200);

        if (hub is IAsyncDisposable ad)
            await ad.DisposeAsync();
        await provider.DisposeAsync();

        // Должно завершиться без исключений
    }

    [TestMethod]
    public async Task PublishingAfterDispose_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddEventHub();

        var provider = services.BuildServiceProvider();
        var hub = provider.GetRequiredService<IEventHub>();

        if (hub is IAsyncDisposable ad)
            await ad.DisposeAsync();

        // Не должно выбрасывать исключений – сообщение просто игнорируется
        await hub.PublishAsync(new TestMessage());

        // Можно дополнительно проверить, что обработчики не вызываются,
        // если нужно убедиться, что сообщение действительно потеряно.
    }

    //[TestMethod]
    //public async Task PublishingAfterDispose_ThrowsChannelClosed()
    //{
    //    var services = new ServiceCollection();
    //    services.AddLogging(b => b.AddConsole());
    //    services.AddEventHub();

    //    var provider = services.BuildServiceProvider();
    //    var hub = provider.GetRequiredService<IEventHub>();

    //    if (hub is IAsyncDisposable ad)
    //        await ad.DisposeAsync();

    //    try
    //    {
    //        await hub.PublishAsync(new TestMessage());
    //        Assert.Fail("Expected ChannelClosedException was not thrown.");
    //    }
    //    catch (ChannelClosedException)
    //    {
    //        // ожидаемое исключение – тест пройден
    //    }
    //}

    //[TestMethod]
    //public async Task PublishingAfterDispose_ThrowsChannelClosed()
    //{
    //    var services = new ServiceCollection();
    //    services.AddLogging(b => b.AddConsole());
    //    services.AddEventHub();

    //    var provider = services.BuildServiceProvider();
    //    var hub = provider.GetRequiredService<IEventHub>();

    //    if (hub is IAsyncDisposable ad)
    //        await ad.DisposeAsync();

    //    // Попытка публикации после Dispose
    //    await Assert.ThrowsExceptionAsync<ChannelClosedException>(async () =>
    //    {
    //        await hub.PublishAsync(new TestMessage());
    //    });
    //}
}