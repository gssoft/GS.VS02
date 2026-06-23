// BackgroundServiceBase.cs
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract class BackgroundServiceBase : BackgroundService
{
    protected readonly GlobalEventHub GlobalHub;
    protected readonly MicroEventBus MicroBus;
    protected readonly ILogger Logger;
    protected readonly string ServiceId; // Уникальный ID сервиса

    // Входящий канал для этого конкретного сервиса
    private readonly Channel<IApplicationEvent> _incomingChannel = Channel.CreateUnbounded<IApplicationEvent>();

    protected BackgroundServiceBase(
        GlobalEventHub globalHub,
        MicroEventBus microBus,
        ILogger logger,
        string serviceId)
    {
        GlobalHub = globalHub;
        MicroBus = microBus;
        Logger = logger;
        ServiceId = serviceId;

        // Регистрируем канал этого сервиса в глобальном хабе при создании экземпляра
        GlobalHub.RegisterServiceChannel(ServiceId, _incomingChannel);
    }

    // Метод, который могут вызывать другие сервисы для отправки события этому сервису
    public async Task SendAsync(IApplicationEvent @event)
    {
        await GlobalHub.PublishToServiceAsync(ServiceId, @event);
    }

    // Основной цикл работы сервиса: читает из своего канала и обрабатывает события локально
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Service {ServiceId} is starting.", ServiceId);

        await foreach (var @event in _incomingChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                Logger.LogDebug("Service {ServiceId} received event of type {EventType}", ServiceId, @event.GetType().Name);
                await MicroBus.PublishAsync(@event); // Обработка в контексте этого сервиса!
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing event in service {ServiceId}", ServiceId);
                // Здесь можно реализовать логику повторных попыток или dead-letter queue
            }
        }

        Logger.LogInformation("Service {ServiceId} is stopping.", ServiceId);
    }
}
