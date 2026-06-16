// Worker.cs - ИСПРАВЛЕННАЯ ВЕРСИЯ
// Firts Version
// Worker.cs - Одиночный цикл с генерацией случайных событий
using WorkerEventBus.Events;

namespace WorkerEventBus;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly EventBus _eventBus;

    public Worker(ILogger<Worker> logger, EventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started");

        // ✅ Публикуем каждое событие с правильным типом
        _logger.LogInformation("🚀 Publishing all events...");

        await _eventBus.PublishAsync(new EventA("First message"));
        await _eventBus.PublishAsync(new EventB(42));
        await _eventBus.PublishAsync(new EventC(true));
        await _eventBus.PublishAsync(new EventA("Second message"));
        await _eventBus.PublishAsync(new EventB(100));
        await _eventBus.PublishAsync(new EventC(false));
        await _eventBus.PublishAsync(new EventA("Third message"));

        _logger.LogInformation("✅ All events published, waiting for processing...");

        // Ждем 5 секунд для завершения обработки
        await Task.Delay(5000, stoppingToken);

        // Завершаем EventBus
        await _eventBus.CompleteAsync();

        _logger.LogInformation("Worker finished");
    }
}

//// Worker.cs
//using WorkerEventBus.Events;

//namespace WorkerEventBus;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly EventBus _eventBus;

//    public Worker(ILogger<Worker> logger, EventBus eventBus)
//    {
//        _logger = logger;
//        _eventBus = eventBus;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Worker started");

//        // Демонстрация параллельной обработки
//        var events = new List<IEvent>
//        {
//            new EventA("First message"),
//            new EventB(42),
//            new EventC(true),
//            new EventA("Second message"),
//            new EventB(100),
//            new EventC(false),
//            new EventA("Third message")
//        };

//        // Публикуем все события "почти одновременно"
//        _logger.LogInformation("🚀 Publishing all events...");

//        var publishTasks = events.Select(e => _eventBus.PublishAsync(e));
//        await Task.WhenAll(publishTasks);

//        _logger.LogInformation("✅ All events published, waiting for processing...");

//        // Ждем 5 секунд для завершения обработки
//        await Task.Delay(5000, stoppingToken);

//        // Завершаем EventBus
//        await _eventBus.CompleteAsync();

//        _logger.LogInformation("Worker finished");
//    }
//}

//namespace WorkerEventBusTpl01
//{
//    public class Worker(ILogger<Worker> logger) : BackgroundService
//    {
//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                if (logger.IsEnabled(LogLevel.Information))
//                {
//                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//                }
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//    }
//}
