// Services/Service2.cs

class Service2 : BackgroundService
{
    private readonly IMessageBus _bus;
    private int _eventCounter = 1;
    private readonly ILogger<Service2> _logger;

    public Service2(IMessageBus bus, ILogger<Service2> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service2 started sending events...");

        // Цикл работает, пока не получен сигнал об остановке от хоста.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Создаем событие.
            var eventMsg = new SomeEvent(
                $"Service2Event-{_eventCounter}",
                DateTime.UtcNow
            );

            // Создаем локальный токен для операции публикации.
            // Это хорошая практика для управления отменой конкретной операции.
            using var publishCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                // Публикуем событие. Токен передаем, чтобы операция могла быть отменена,
                // если она поддерживает отмену.
                await _bus.PublishAsync(eventMsg, publishCts.Token);
                _eventCounter++;

                // Ключевое изменение: убираем stoppingToken из Task.Delay.
                // Это позволяет задержке завершиться, давая сервису время на "плавную" остановку.
                await Task.Delay(TimeSpan.FromSeconds(7));
            }
            catch (OperationCanceledException) when (publishCts.IsCancellationRequested)
            {
                // Логируем, если была отменена сама операция публикации.
                _logger.LogWarning("Publishing of event {EventId} was cancelled.", eventMsg);
            }
            catch (Exception ex)
            {
                // Логируем любые другие ошибки, чтобы сервис не "падал".
                _logger.LogError(ex, "Error occurred while publishing event {EventId}.", eventMsg);
            }
        }

        // Этот лог будет выведен после того, как цикл while завершится,
        // что произойдет после получения сигнала на остановку и завершения текущей итерации.
        _logger.LogInformation("Service2 is shutting down gracefully...");
    }
}


