// Worker.cs - Бесконечный цикл с генерацией случайных событий
// Интенсивный вариант
// Полный рабочий Worker.cs (с поддержкой CancellationToken):
// Worker.cs - Интенсивная версия с тремя генераторами
using System.Diagnostics;
using WorkerEventBus.Events;

namespace WorkerEventBus;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly EventBus _eventBus;
    private readonly Random _random = new();
    private int _eventCounter;
    private readonly Stopwatch _stopwatch = new();
    private Timer? _monitoringTimer;
    private readonly CancellationTokenSource _internalCts = new();

    public Worker(ILogger<Worker> logger, EventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Worker started - HIGH LOAD MODE with 3 parallel generators");
        _logger.LogInformation("Press Ctrl+C to stop...");
        _logger.LogInformation("");

        _stopwatch.Start();

        // Запускаем мониторинг каждые 5 секунд
        _monitoringTimer = new Timer(_ => MonitorStats(), null, 5000, 5000);

        // Создаем linked token source для отмены всех генераторов
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _internalCts.Token);

        // Запускаем три параллельных генератора событий
        var generators = new[]
        {
            GenerateEvents("Generator-1", linkedCts.Token),
            GenerateEvents("Generator-2", linkedCts.Token),
            GenerateEvents("Generator-3", linkedCts.Token)
        };

        try
        {
            // Ждем завершения всех генераторов (будет при Ctrl+C)
            await Task.WhenAll(generators);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ Cancellation requested, shutting down...");
        }
        finally
        {
            _monitoringTimer?.Dispose();
            await _eventBus.CompleteAsync();
            _stopwatch.Stop();

            _logger.LogInformation("");
            _logger.LogInformation("📊 FINAL STATISTICS:");
            _logger.LogInformation("   Total events generated: {Counter:N0}", _eventCounter);
            _logger.LogInformation("   Runtime: {Elapsed}", _stopwatch.Elapsed);
            _logger.LogInformation("   Average rate: {Rate:F2} events/sec", _eventCounter / _stopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("🏁 Worker finished");
        }
    }
    // Intensive
    private async Task GenerateEvents(string generatorName, CancellationToken stoppingToken)
    {
        var eventTypes = new[] { "A", "B", "C" };
        var messages = new[] { "Hello", "World", "Test", "Message", "Data", "Info", "Event", "Info" };
        var localCounter = 0;

        _logger.LogInformation("[{Generator}] Started generating events", generatorName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var eventType = eventTypes[_random.Next(eventTypes.Length)];
                var eventNumber = Interlocked.Increment(ref _eventCounter);
                localCounter++;

                _logger.LogDebug("[{Generator}] Generating event #{Counter} of type {EventType}",
                    generatorName, eventNumber, eventType);

                switch (eventType)
                {
                    case "A":
                        var message = messages[_random.Next(messages.Length)];
                        await _eventBus.PublishAsync(new EventA($"[{generatorName}] {message} #{eventNumber}"), stoppingToken);
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

                // Более высокая интенсивность - задержка 10-100ms
                var delay = _random.Next(10, 100);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{Generator}] Stopped. Generated {Count} events", generatorName, localCounter);
        }
    }

    // Super Inensive !!!!!!!!!!!!!!!!!!!!
    // Супер-интенсивная версия без задержек
    //private async Task GenerateEvents(string generatorName, CancellationToken stoppingToken)
    //{
    //    var eventTypes = new[] { "A", "B", "C" };
    //    var messages = new[] { "Hello", "World", "Test", "Message", "Data", "Info" };
    //    var localCounter = 0;
    //    var batchSize = _random.Next(1, 10); // Пакетная отправка

    //    _logger.LogInformation("[{Generator}] Started generating events (SUPER INTENSIVE MODE)", generatorName);

    //    try
    //    {
    //        while (!stoppingToken.IsCancellationRequested)
    //        {
    //            // Отправляем пакет событий
    //            var tasks = new List<Task>();

    //            for (int i = 0; i < batchSize; i++)
    //            {
    //                var eventType = eventTypes[_random.Next(eventTypes.Length)];
    //                var eventNumber = Interlocked.Increment(ref _eventCounter);
    //                localCounter++;

    //                switch (eventType)
    //                {
    //                    case "A":
    //                        var message = messages[_random.Next(messages.Length)];
    //                        tasks.Add(_eventBus.PublishAsync(new EventA($"[{generatorName}] {message} #{eventNumber}"), stoppingToken));
    //                        break;

    //                    case "B":
    //                        var number = _random.Next(1, 1000);
    //                        tasks.Add(_eventBus.PublishAsync(new EventB(number), stoppingToken));
    //                        break;

    //                    case "C":
    //                        var flag = _random.Next(2) == 0;
    //                        tasks.Add(_eventBus.PublishAsync(new EventC(flag), stoppingToken));
    //                        break;
    //                }
    //            }

    //            await Task.WhenAll(tasks);

    //            // Минимальная задержка или вообще без задержки
    //            if (batchSize > 5)
    //                await Task.Delay(5, stoppingToken);

    //            batchSize = _random.Next(1, 10);
    //        }
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        _logger.LogInformation("[{Generator}] Stopped. Generated {Count} events", generatorName, localCounter);
    //    }
    //}

    private void MonitorStats()
    {
        var stats = _eventBus.GetStats();
        var elapsed = _stopwatch.Elapsed;
        var rate = _eventCounter / elapsed.TotalSeconds;

        _logger.LogInformation(
            "📊 [MONITOR] Runtime: {Elapsed:F1}s | Events: {Total:N0} | Rate: {Rate:F1} ev/s | Queues: A={QueueA}, B={QueueB}, C={QueueC}",
            elapsed.TotalSeconds,
            _eventCounter,
            rate,
            stats.QueueA,
            stats.QueueB,
            stats.QueueC
        );

        // Предупреждение о переполнении очередей
        if (stats.QueueA > 50) _logger.LogWarning("⚠️ Queue A is growing: {QueueA}", stats.QueueA);
        if (stats.QueueB > 50) _logger.LogWarning("⚠️ Queue B is growing: {QueueB}", stats.QueueB);
        if (stats.QueueC > 50) _logger.LogWarning("⚠️ Queue C is growing: {QueueC}", stats.QueueC);
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
//                var eventType = eventTypes[_random.Next(eventTypes.Length)];
//                _eventCounter++;

//                _logger.LogInformation("🔄 Generating event #{Counter} of type {EventType}", _eventCounter, eventType);

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
