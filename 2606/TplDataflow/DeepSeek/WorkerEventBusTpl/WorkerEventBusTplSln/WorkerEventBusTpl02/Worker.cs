// Worker.cs - Бесконечный цикл с генерацией случайных событий
// Полный рабочий Worker.cs (с поддержкой CancellationToken):
// 

using WorkerEventBus.Events;

namespace WorkerEventBus;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly EventBus _eventBus;
    private readonly Random _random = new();
    private int _eventCounter;

    public Worker(ILogger<Worker> logger, EventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Worker started - Infinite event generation mode");
        _logger.LogInformation("Press Ctrl+C to stop...");
        _logger.LogInformation("");

        var eventTypes = new[] { "A", "B", "C" };
        var messages = new[] { "Hello", "World", "Test", "Message", "Data", "Info" };

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var eventType = eventTypes[_random.Next(eventTypes.Length)];
                _eventCounter++;

                _logger.LogInformation("🔄 Generating event #{Counter} of type {EventType}", _eventCounter, eventType);

                switch (eventType)
                {
                    case "A":
                        var message = messages[_random.Next(messages.Length)];
                        await _eventBus.PublishAsync(new EventA($"{message} #{_eventCounter}"), stoppingToken);
                        break;

                    case "B":
                        var number = _random.Next(1, 1000);
                        await _eventBus.PublishAsync(new EventB(number), stoppingToken);
                        break;

                    case "C":
                        var flag = _random.Next(2) == 0;
                        await _eventBus.PublishAsync(new EventC(flag), stoppingToken);
                        break;
                }

                var delay = _random.Next(100, 500);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ Cancellation requested, shutting down...");
        }
        finally
        {
            await _eventBus.CompleteAsync();
            _logger.LogInformation("🏁 Worker finished. Total events generated: {Counter}", _eventCounter);
        }
    }
}

//using WorkerEventBus.Events;

//namespace WorkerEventBus;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly EventBus _eventBus;
//    private readonly Random _random = new();
//    private int _eventCounter;

//    public Worker(ILogger<Worker> logger, EventBus eventBus)
//    {
//        _logger = logger;
//        _eventBus = eventBus;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("🚀 Worker started - Infinite event generation mode");
//        _logger.LogInformation("Press Ctrl+C to stop...");
//        _logger.LogInformation("");

//        var eventTypes = new[] { "A", "B", "C" };
//        var messages = new[] { "Hello", "World", "Test", "Message", "Data", "Info" };

//        try
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                // Генерируем случайное событие
//                var eventType = eventTypes[_random.Next(eventTypes.Length)];
//                _eventCounter++;

//                _logger.LogInformation("🔄 Generating event #{Counter} of type {EventType}", _eventCounter, eventType);

//                // Создаем и публикуем событие
//                switch (eventType)
//                {
//                    case "A":
//                        var message = messages[_random.Next(messages.Length)];
//                        await _eventBus.PublishAsync(new EventA($"{message} #{_eventCounter}"), stoppingToken);
//                        break;

//                    case "B":
//                        var number = _random.Next(1, 1000);
//                        await _eventBus.PublishAsync(new EventB(number), stoppingToken);
//                        break;

//                    case "C":
//                        var flag = _random.Next(2) == 0;
//                        await _eventBus.PublishAsync(new EventC(flag), stoppingToken);
//                        break;
//                }

//                // Случайная задержка между событиями (100-500ms)
//                var delay = _random.Next(100, 500);
//                await Task.Delay(delay, stoppingToken);
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            _logger.LogInformation("⏹️ Cancellation requested, shutting down...");
//        }
//        finally
//        {
//            await _eventBus.CompleteAsync();
//            _logger.LogInformation("🏁 Worker finished. Total events generated: {Counter}", _eventCounter);
//        }
//    }
//}


//// Worker.cs - 1 ИСПРАВЛЕННАЯ ВЕРСИЯ
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

//        // ✅ Публикуем каждое событие с правильным типом
//        _logger.LogInformation("🚀 Publishing all events...");

//        await _eventBus.PublishAsync(new EventA("First message"));
//        await _eventBus.PublishAsync(new EventB(42));
//        await _eventBus.PublishAsync(new EventC(true));
//        await _eventBus.PublishAsync(new EventA("Second message"));
//        await _eventBus.PublishAsync(new EventB(100));
//        await _eventBus.PublishAsync(new EventC(false));
//        await _eventBus.PublishAsync(new EventA("Third message"));

//        _logger.LogInformation("✅ All events published, waiting for processing...");

//        // Ждем 5 секунд для завершения обработки
//        await Task.Delay(5000, stoppingToken);

//        // Завершаем EventBus
//        await _eventBus.CompleteAsync();

//        _logger.LogInformation("Worker finished");
//    }
//}
