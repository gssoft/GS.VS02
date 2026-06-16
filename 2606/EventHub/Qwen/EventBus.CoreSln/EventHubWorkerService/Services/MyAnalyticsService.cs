// Services/MyAnalyticsService.cs

using System.Threading.Channels;
using EventBus.Abstractions;
using EventBus.Core;
using EventBus.Hosting;
using EventHubWorkerService.Messages;
using Microsoft.Extensions.Logging;

namespace EventHubWorkerService.Services;

public class MyAnalyticsService : EventSubscriberService<MyEvent>
{
    private readonly IEventBus _bus;
    private readonly Channel<MyEvent> _inbox;

    public MyAnalyticsService(
        IEventBus bus,
        Channel<MyEvent> inbox,
        IHandler<MyEvent> handler,
        ILogger<MyAnalyticsService> logger)
        : base(inbox, logger, handler)
    {
        _bus = bus;
        _inbox = inbox;

        // Подписываем свой канал на шину
        // Приводим к конкретному типу, чтобы получить доступ к Subscribe
        if (bus is ChannelEventBus channelBus)
        {
            channelBus.Subscribe(_inbox.Writer);
        }
    }
}
