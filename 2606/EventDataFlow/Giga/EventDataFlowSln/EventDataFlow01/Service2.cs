// Service2.cs (Consumer)
public class Service2 : BackgroundServiceBase
{
    public Service2(GlobalEventHub globalHub, MicroEventBus microBus, ILogger<Service2> logger)
        : base(globalHub, microBus, logger, "Service2")
    {
        // Здесь происходит подписка на интересующие нас типы событий!
        // Эта подписка локальна для Service2.
        MicroBus.Subscribe<UserRegisteredEvent>(HandleUserRegistered);
    }

    // Основной цикл ExecuteAsync у Consumer-а фактически пустой,
    // вся работа происходит в обработчиках событий MicroBus-а.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Consumer Service2 is running and waiting for events...");

        // Просто ждем событий из канала. Вся логика - в подписках MicroBus.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleUserRegistered(UserRegisteredEvent @event)
    {
        // ВАЖНО: Этот код выполняется в контексте Service2!
        Logger.LogInformation("--- [SERVICE2 HANDLER] New user registered: {Username} ---", @event.Username);

        // Здесь можно использовать скоуп зависимостей IServiceScopeFactory,
        // если нужно работать с DbContext или другими scoped-сервисами.

        return Task.CompletedTask;
    }
}
