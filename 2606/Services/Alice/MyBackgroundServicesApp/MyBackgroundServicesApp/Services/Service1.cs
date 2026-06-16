// Services/Service1.cs

class Service1 : BackgroundService
{
    private readonly IMessageBus _bus;
    private int _counter = 1;
    private readonly ILogger<Service1> _logger;

    public Service1(IMessageBus bus, ILogger<Service1> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service1 started sending commands...");

        // Цикл продолжается, пока не получен сигнал об остановке от хоста.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Создаем команду. Это легкая операция, ее можно делать всегда.
            var command = new SomeCommand(_counter, $"Data from Service1 - {DateTime.UtcNow}");

            // Создаем локальный токен для операции отправки.
            // Он будет отменен, если либо придет общий сигнал остановки,
            // либо сам метод ExecuteAsync будет завершен.
            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                // Отправляем команду. Если операция займет много времени,
                // ее можно будет отменить по sendCts.Token.
                await _bus.SendAsync(command, sendCts.Token);
                _counter++;

                // Задержка на 5 секунд. Токен НЕ передаем!
                // Это ключевой момент: мы даем циклу завершиться естественно.
                // Если придет сигнал остановки, Task.Delay просто "проснется"
                // и цикл проверит условие while на следующей итерации.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException) when (sendCts.IsCancellationRequested)
            {
                // Эта секция сработает, если сама операция SendAsync была отменена.
                // Например, если шина сообщений поддерживает отмену и мы ее запросили.
                // В этом случае мы просто переходим к следующей итерации или выходим из цикла.
                _logger.LogWarning("Sending of command {CommandId} was cancelled.", command.Id);
            }
            catch (Exception ex)
            {
                // Логируем любые другие ошибки, но не даем им остановить сервис.
                _logger.LogError(ex, "Error occurred while sending command {CommandId}.", command.Id);
            }
        }

        // Этот лог будет выведен, когда цикл while завершится,
        // то есть после получения сигнала stoppingToken.IsCancellationRequested = true
        // и завершения последней итерации цикла.
        _logger.LogInformation("Service1 is shutting down gracefully...");
    }
}


