// Service1.cs (Producer)
public class Service1 : BackgroundServiceBase
{
    private int _counter = 0;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

    public Service1(GlobalEventHub globalHub, MicroEventBus microBus, ILogger<Service1> logger)
        : base(globalHub, microBus, logger, "Service1") // Передаем уникальный ID

    {
        // Подписываемся на свои собственные события (если нужно)
        // MicroBus.Subscribe<SomeLocalEvent>(HandleLocalEvent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Producer Service1 is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _counter++;
            var newUserEvent = new UserRegisteredEvent($"User_{_counter}");

            Logger.LogInformation("Service1 is publishing event to Service2.");

            // Отправляем событие напрямую в Глобальный Хаб, указывая ID целевого сервиса!
            await GlobalHub.PublishToServiceAsync("Service2", newUserEvent);

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
