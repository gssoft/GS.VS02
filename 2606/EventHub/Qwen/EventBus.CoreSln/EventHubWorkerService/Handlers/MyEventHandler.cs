// Handlers/MyEventHandler.cs

using EventBus.Abstractions;
using EventHubWorkerService.Messages;
using Microsoft.Extensions.Logging;

namespace EventHubWorkerService.Handlers;

public class MyEventHandler : IHandler<MyEvent>
{
    private readonly ILogger<MyEventHandler> _logger;

    public MyEventHandler(ILogger<MyEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(MyEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("✅ [Handler] Received event: {Id} | {Payload}",
            @event.Id, @event.Payload);

        // Имитация асинхронной работы (БД, сеть и т.д.)
        // Обратите внимание: это выполняется в контексте MyAnalyticsService!
        return Task.Delay(100, cancellationToken);
    }

    // В MyEventHandler.cs Проверка что работает в контексте Subscribe
    //public async Task HandleAsync(MyEvent @event, CancellationToken ct)
    //{
    //    _logger.LogInformation("✅ Start handling: {Id}", @event.Id);

    //    // Имитация "тяжёлой" работы в подписчике (2 секунды)
    //    await Task.Delay(TimeSpan.FromSeconds(2), ct);

    //    _logger.LogInformation("✅ Finished handling: {Id}", @event.Id);
    //}

}
