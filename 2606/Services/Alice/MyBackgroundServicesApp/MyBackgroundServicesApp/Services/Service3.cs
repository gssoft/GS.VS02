// Services/Service3.cs

class Service3 : BackgroundService
{
    private readonly IMessageBus _bus;
    private int _commandCounter = 1;
    private readonly ILogger<Service3> _logger;

    public Service3(IMessageBus bus, ILogger<Service3> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service3 started sending commands...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var command = new AnotherCommand(
                Guid.NewGuid(),
                DateTime.UtcNow
            );

            // Используем локальный токен для операции отправки
            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                // Отправка команды с возможностью отмены
                await _bus.SendAsync(command, sendCts.Token);
                _commandCounter++;

                // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Убираем stoppingToken из Task.Delay.
                // Это позволяет задержке завершиться, давая сервису время на "плавную" остановку.
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException) when (sendCts.IsCancellationRequested)
            {
                // Логируем, если была отменена сама операция отправки.
                _logger.LogWarning("Sending of command {CommandId} was cancelled.", command);
            }
            catch (Exception ex)
            {
                // Логируем непредвиденные ошибки, чтобы сервис не "падал".
                _logger.LogError(ex, "Error occurred while sending command {CommandId}.", command);
            }
        }

        // Этот лог будет выведен после того, как цикл while завершится,
        // что произойдет после получения сигнала на остановку и завершения текущей итерации.
        _logger.LogInformation("Service3 is shutting down gracefully...");
    }
}

