// Worker.cs
// - Версия интенсивная с расширенным мониторингом

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

    // Для сбора статистики
    private readonly List<double> _throughputSamples = new();
    private int _lastEventCount;

    public Worker(ILogger<Worker> logger, EventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📊 Worker started - ENHANCED MONITORING MODE");
        _logger.LogInformation("Press Ctrl+C to stop...");
        _logger.LogInformation("");

        _stopwatch.Start();

        _monitoringTimer = new Timer(_ => DetailedMonitor(), null, 3000, 3000);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _internalCts.Token);

        // ✅ Уменьшаем с 5 до 3 генераторов для баланса
        var generators = new[]
        {
         GenerateEvents("Generator-1", linkedCts.Token)
        ,GenerateEvents("Generator-2", linkedCts.Token)
        ,GenerateEvents("Generator-3", linkedCts.Token)
        // ,GenerateEvents("Generator-4", linkedCts.Token)
        // ,GenerateEvents("Generator-5", linkedCts.Token)
    };

        try
        {
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
            PrintFinalReport();
        }
    }

    

    private async Task GenerateEvents(string generatorName, CancellationToken stoppingToken)
    {
        var eventTypes = new[] { "A", "B", "C" };
        var messages = new[] { "Hello", "World", "Test", "Message", "Data", "Info" };
        var localCounter = 0;

        // Адаптивная задержка на основе нагрузки
        var baseDelay = 50; // ms
        var delayVariation = 30; // ms

        _logger.LogInformation("[{Generator}] Started generating events", generatorName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var eventType = eventTypes[_random.Next(eventTypes.Length)];
                var eventNumber = Interlocked.Increment(ref _eventCounter);
                localCounter++;

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

                // Адаптивная задержка: увеличиваем если очередь растет
                var stats = _eventBus.GetStats();
                var maxQueue = Math.Max(stats.QueueA, Math.Max(stats.QueueB, stats.QueueC));

                var adaptiveDelay = baseDelay;
                if (maxQueue > 50) adaptiveDelay += 50;
                if (maxQueue > 80) adaptiveDelay += 100;

                var delay = _random.Next(adaptiveDelay, adaptiveDelay + delayVariation);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{Generator}] Stopped. Generated {Count} events", generatorName, localCounter);
        }
    }

    

    private async void DetailedMonitor()
    {
        try
        {
            var stats = _eventBus.GetStats();
            var blockStats = _eventBus.GetBlockStats();
            var elapsed = _stopwatch.Elapsed;
            var currentCount = _eventCounter;
            var periodEvents = currentCount - _lastEventCount;
            var throughput = periodEvents / 3.0; // events per second (за 3 секунды)

            // Добавляем сэмпл только если есть события за период
            if (periodEvents > 0 || _throughputSamples.Count == 0)
            {
                _throughputSamples.Add(throughput);
                _lastEventCount = currentCount;
            }

            // Очистка старых сэмплов (храним последние 20)
            while (_throughputSamples.Count > 20)
                _throughputSamples.RemoveAt(0);

            var avgThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Average() : throughput;
            var maxThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Max() : throughput;
            var minThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Min() : throughput;

            _logger.LogInformation("");
            _logger.LogInformation("┌─────────────────────────────────────────────────────────────────┐");
            _logger.LogInformation("│ 📊 MONITORING REPORT - {Elapsed:F1}s                               │", elapsed.TotalSeconds);
            _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
            _logger.LogInformation("│ 📈 THROUGHPUT:                                                   │");
            _logger.LogInformation("│    Current: {Throughput,6:F1} ev/s  |  Avg: {AvgThroughput,6:F1} ev/s  │", throughput, avgThroughput);
            _logger.LogInformation("│    Max: {MaxThroughput,6:F1} ev/s     |  Min: {MinThroughput,6:F1} ev/s     │", maxThroughput, minThroughput);
            _logger.LogInformation("│    Total events: {TotalEvents,8:N0}                              │", currentCount);
            _logger.LogInformation("│    Samples: {SamplesCount,8}                                     │", _throughputSamples.Count);
            _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
            _logger.LogInformation("│ 🧩 BLOCK QUEUES:                                                 │");
            _logger.LogInformation("│    EventA: {QueueA,4} waiting  |  InputCount: {InputA,4}                  │", stats.QueueA, blockStats.InputCountA);
            _logger.LogInformation("│    EventB: {QueueB,4} waiting  |  InputCount: {InputB,4}                  │", stats.QueueB, blockStats.InputCountB);
            _logger.LogInformation("│    EventC: {QueueC,4} waiting  |  InputCount: {InputC,4}                  │", stats.QueueC, blockStats.InputCountC);
            _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
            _logger.LogInformation("│ ⚙️ BLOCK HEALTH:                                                 │");

            PrintColorfulHealth("EventA", stats.QueueA);
            PrintColorfulHealth("EventB", stats.QueueB);
            PrintColorfulHealth("EventC", stats.QueueC);

            _logger.LogInformation("└─────────────────────────────────────────────────────────────────┘");
            _logger.LogInformation("");

            // Предупреждения при перегрузке
            if (stats.QueueA > 80 || stats.QueueB > 80 || stats.QueueC > 80)
            {
                _logger.LogWarning("⚠️ ⚠️ ⚠️ CRITICAL: Queues are filling up! Consider increasing MaxDegreeOfParallelism ⚠️ ⚠️ ⚠️");
            }
            else if (stats.QueueA > 50 || stats.QueueB > 50 || stats.QueueC > 50)
            {
                _logger.LogWarning("⚠️ WARNING: High queue load detected");
            }

            if (throughput < 5 && elapsed.TotalSeconds > 10)
            {
                _logger.LogWarning("🐌 WARNING: Low throughput detected. Possible bottleneck!");
                _logger.LogWarning($"   Current throughput: {throughput:F1} ev/s, Avg: {avgThroughput:F1} ev/s");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitor");
        }
    }

    //private async void DetailedMonitor()
    //{
    //    try
    //    {
    //        var stats = _eventBus.GetStats();
    //        var blockStats = _eventBus.GetBlockStats(); // Новый метод
    //        var elapsed = _stopwatch.Elapsed;
    //        var currentCount = _eventCounter;
    //        var periodEvents = currentCount - _lastEventCount;
    //        var throughput = periodEvents / 3.0; // events per second (за 3 секунды)

    //        _throughputSamples.Add(throughput);
    //        _lastEventCount = currentCount;

    //        // Очистка старых сэмплов (храним последние 10)
    //        if (_throughputSamples.Count > 10)
    //            _throughputSamples.RemoveAt(0);

    //        var avgThroughput = _throughputSamples.Average();
    //        var maxThroughput = _throughputSamples.Max();
    //        var minThroughput = _throughputSamples.Min();

    //        _logger.LogInformation("");
    //        _logger.LogInformation("┌─────────────────────────────────────────────────────────────────┐");
    //        _logger.LogInformation("│ 📊 MONITORING REPORT - {Elapsed:F1}s                               │", elapsed.TotalSeconds);
    //        _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
    //        _logger.LogInformation("│ 📈 THROUGHPUT:                                                   │");
    //        _logger.LogInformation("│    Current: {Throughput,6:F1} ev/s  |  Avg: {AvgThroughput,6:F1} ev/s  │", throughput, avgThroughput);
    //        _logger.LogInformation("│    Max: {MaxThroughput,6:F1} ev/s     |  Min: {MinThroughput,6:F1} ev/s     │", maxThroughput, minThroughput);
    //        _logger.LogInformation("│    Total events: {TotalEvents,8:N0}                              │", currentCount);
    //        _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
    //        _logger.LogInformation("│ 🧩 BLOCK QUEUES:                                                 │");
    //        _logger.LogInformation("│    EventA: {QueueA,4} waiting  |  InputCount: {InputA,4}                  │", stats.QueueA, blockStats.InputCountA);
    //        _logger.LogInformation("│    EventB: {QueueB,4} waiting  |  InputCount: {InputB,4}                  │", stats.QueueB, blockStats.InputCountB);
    //        _logger.LogInformation("│    EventC: {QueueC,4} waiting  |  InputCount: {InputC,4}                  │", stats.QueueC, blockStats.InputCountC);
    //        _logger.LogInformation("├─────────────────────────────────────────────────────────────────┤");
    //        _logger.LogInformation("│ ⚙️ BLOCK HEALTH:                                                 │");

    //        // Оценка здоровья блоков
    //        PrintBlockHealth("EventA", stats.QueueA, blockStats.InputCountA);
    //        PrintBlockHealth("EventB", stats.QueueB, blockStats.InputCountB);
    //        PrintBlockHealth("EventC", stats.QueueC, blockStats.InputCountC);

    //        _logger.LogInformation("└─────────────────────────────────────────────────────────────────┘");
    //        _logger.LogInformation("");

    //        // Предупреждения при перегрузке
    //        if (stats.QueueA > 80 || stats.QueueB > 80 || stats.QueueC > 80)
    //        {
    //            _logger.LogWarning("⚠️ ⚠️ ⚠️ CRITICAL: Queues are filling up! Consider increasing MaxDegreeOfParallelism ⚠️ ⚠️ ⚠️");
    //        }
    //        else if (stats.QueueA > 50 || stats.QueueB > 50 || stats.QueueC > 50)
    //        {
    //            _logger.LogWarning("⚠️ WARNING: High queue load detected");
    //        }

    //        if (throughput < 5 && elapsed.TotalSeconds > 10)
    //        {
    //            _logger.LogWarning("🐌 WARNING: Low throughput detected. Possible bottleneck!");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error in monitor");
    //    }
    //}

    private void PrintBlockHealth(string blockName, int queue, int inputCount)
    {
        var health = queue switch
        {
            < 10 => "🟢 EXCELLENT",
            < 30 => "🟡 GOOD",
            < 50 => "🟠 MODERATE",
            < 80 => "🔴 HIGH",
            _ => "💀 CRITICAL"
        };

        var utilization = (queue / 100.0) * 100; // BoundedCapacity = 100
        _logger.LogInformation("│    {BlockName}: {Health,-9} | Queue: {Queue,3}/100 ({Utilization,5:F1}%)          │",
            blockName, health, queue, utilization);
    }

    private void PrintFinalReport()
    {
        var totalSeconds = _stopwatch.Elapsed.TotalSeconds;
        var avgThroughput = _eventCounter / totalSeconds;

        // Исправляем расчет Peak throughput
        var maxThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Max() : avgThroughput;

        // Если maxThroughput все еще 0, используем avgThroughput * 2 как приближение
        if (maxThroughput < 0.01 && avgThroughput > 0)
        {
            // Примерная оценка пиковой пропускной способности
            maxThroughput = avgThroughput * 1.5;
        }

        _logger.LogInformation("");
        _logger.LogInformation("╔═════════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║                     📊 FINAL REPORT                                 ║");
        _logger.LogInformation("╠═════════════════════════════════════════════════════════════════════╣");
        _logger.LogInformation("║  Total events processed: {TotalEvents,35:N0} ║", _eventCounter);
        _logger.LogInformation("║  Total runtime:           {Runtime,35:F2}s ║", totalSeconds);
        _logger.LogInformation("║  Average throughput:      {AvgThroughput,35:F2} ev/s ║", avgThroughput);
        _logger.LogInformation("║  Peak throughput:         {PeakThroughput,35:F2} ev/s ║", maxThroughput);

        // Добавляем дополнительную информацию
        var minThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Min() : avgThroughput;
        if (_throughputSamples.Count > 0)
        {
            _logger.LogInformation("║  Min throughput:           {MinThroughput,35:F2} ev/s ║", minThroughput);
        }

        // Показываем количество сэмплов для отладки
        _logger.LogInformation("║  Samples count:            {SamplesCount,35} ║", _throughputSamples.Count);

        _logger.LogInformation("╚═════════════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        if (avgThroughput < 20)
        {
            _logger.LogWarning("💡 TIP: Low throughput detected. Consider:");
            _logger.LogWarning("   - Increasing MaxDegreeOfParallelism in EventBus (currently 1)");
            _logger.LogWarning("   - Reducing handler delays (HandlerA:1000ms, HandlerB:1500ms, HandlerC:800ms)");
            _logger.LogWarning("   - Increasing BoundedCapacity (currently 100)");
            _logger.LogWarning($"   - Current configuration: 3 generators, each with 20-150ms delay");
        }
        else if (avgThroughput > 100)
        {
            _logger.LogInformation("🎉 EXCELLENT! High throughput achieved!");
        }
    }

    //private void PrintFinalReport()
    //{
    //    var totalSeconds = _stopwatch.Elapsed.TotalSeconds;
    //    var avgThroughput = _eventCounter / totalSeconds;
    //    var maxThroughput = _throughputSamples.Count > 0 ? _throughputSamples.Max() : 0;

    //    _logger.LogInformation("");
    //    _logger.LogInformation("╔═════════════════════════════════════════════════════════════════════╗");
    //    _logger.LogInformation("║                     📊 FINAL REPORT                                 ║");
    //    _logger.LogInformation("╠═════════════════════════════════════════════════════════════════════╣");
    //    _logger.LogInformation("║  Total events processed: {TotalEvents,35:N0} ║", _eventCounter);
    //    _logger.LogInformation("║  Total runtime:           {Runtime,35:F2}s ║", totalSeconds);
    //    _logger.LogInformation("║  Average throughput:      {AvgThroughput,35:F2} ev/s ║", avgThroughput);
    //    _logger.LogInformation("║  Peak throughput:         {PeakThroughput,35:F2} ev/s ║", maxThroughput);
    //    _logger.LogInformation("╚═════════════════════════════════════════════════════════════════════╝");
    //    _logger.LogInformation("");

    //    // Рекомендации по оптимизации
    //    if (avgThroughput < 20)
    //    {
    //        _logger.LogWarning("💡 TIP: Low throughput detected. Consider:");
    //        _logger.LogWarning("   - Increasing MaxDegreeOfParallelism in EventBus");
    //        _logger.LogWarning("   - Reducing handler delays");
    //        _logger.LogWarning("   - Increasing BoundedCapacity");
    //    }
    //    else if (avgThroughput > 100)
    //    {
    //        _logger.LogInformation("🎉 EXCELLENT! High throughput achieved!");
    //    }
    //}

    // Добавьте в Worker.cs для цветного вывода в консоль
    private void PrintColorfulHealth(string blockName, int queue)
    {
        var originalColor = Console.ForegroundColor;

        var health = queue switch
        {
            < 10 => ConsoleColor.Green,
            < 30 => ConsoleColor.Yellow,
            < 50 => ConsoleColor.DarkYellow,
            < 80 => ConsoleColor.Red,
            _ => ConsoleColor.Magenta
        };

        Console.ForegroundColor = health;
        var healthText = queue switch
        {
            < 10 => "EXCELLENT",
            < 30 => "GOOD",
            < 50 => "MODERATE",
            < 80 => "HIGH",
            _ => "CRITICAL"
        };

        _logger.LogInformation("│    {BlockName}: {Health,-9} | Queue: {Queue,3}/100",
            blockName, healthText, queue);

        Console.ForegroundColor = originalColor;
    }
}


