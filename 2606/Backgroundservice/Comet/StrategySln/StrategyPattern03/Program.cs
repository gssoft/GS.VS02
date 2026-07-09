using System;
using System.Threading;
using System.Threading.Tasks;

#region 1. Интерфейсы стратегий (Контракты)

/// <summary>
/// Стратегия поведения: что именно делает сервис.
/// </summary>
public interface IBehaviorStrategy
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Стратегия планирования: когда и как запускать работу.
/// </summary>
public interface ISchedulerStrategy
{
    Task ScheduleLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction);
}

/// <summary>
/// Стратегия метрик: как измерять производительность.
/// </summary>
public interface IMetricsStrategy
{
    void Begin();
    void End();
}

/// <summary>
/// Стратегия тюнинга: как применять мелкие настройки.
/// </summary>
public interface ITunerStrategy
{
    void ApplyAdjustments();
}

#endregion

#region 2. Конкретные реализации стратегий

/// <summary>
/// Реальное поведение: эмуляция тяжелой работы (например, загрузка данных).
/// </summary>
public class DataProcessingBehavior : IBehaviorStrategy
{
    private readonly int _workTimeMs;
    public DataProcessingBehavior(int workTimeMs) => _workTimeMs = workTimeMs;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Behavior] Начинаем обработку данных...");
        await Task.Delay(_workTimeMs, cancellationToken); // Симуляция тяжелой операции
        Console.WriteLine($"[Behavior] Обработка завершена за {_workTimeMs} мс.");
    }
}

/// <summary>
/// Стратегия расписания: запускает работу каждую секунду.
/// </summary>
public class IntervalScheduler : ISchedulerStrategy
{
    private readonly TimeSpan _interval;
    public IntervalScheduler(TimeSpan interval) => _interval = interval;

    public async Task ScheduleLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine($"[Scheduler] Планируем запуск следующей итерации...");
            await workAction(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }
}

/// <summary>
/// Простая реализация сбора метрик: замеряет время выполнения.
/// </summary>
public class TimingMetrics : IMetricsStrategy
{
    private DateTimeOffset _startTime;
    public void Begin() => _startTime = DateTimeOffset.Now;
    public void End()
    {
        var duration = DateTimeOffset.Now - _startTime;
        Console.WriteLine($"[Metrics] Время выполнения: {duration.TotalMilliseconds:F2} мс.");
    }
}

/// <summary>
/// Стратегия тюнинга: применяет настройки (например, задержку).
/// </summary>
public class PerformanceTuner : ITunerStrategy
{
    private readonly int _delayAdjustmentMs;
    public PerformanceTuner(int delayAdjustmentMs) => _delayAdjustmentMs = delayAdjustmentMs;

    public void ApplyAdjustments()
    {
        Console.WriteLine($"[Tuner] Применяем поправки: задержка уменьшена на {_delayAdjustmentMs} мс.");
    }
}

#endregion

#region 3. Контекст (Главный сервис)

/// <summary>
/// Главный сервис, который объединяет четыре стратегии.
/// Он не реализует логику сам, а делегирует ее компонентам.
/// </summary>
public class BackgroundService : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ISchedulerStrategy _scheduler;
    private readonly IBehaviorStrategy _behavior;
    private readonly IMetricsStrategy _metrics;
    private readonly ITunerStrategy _tuner;
    private Task? _executionTask;

    public BackgroundService(
        ISchedulerStrategy scheduler,
        IBehaviorStrategy behavior,
        IMetricsStrategy metrics,
        ITunerStrategy tuner)
    {
        _scheduler = scheduler;
        _behavior = behavior;
        _metrics = metrics;
        _tuner = tuner;
    }

    public void Start()
    {
        Console.WriteLine("[BackgroundService] Сервис запущен.\n");
        _executionTask = Task.Run(() => _scheduler.ScheduleLoopAsync(_cts.Token, WorkIteration));
    }

    private async Task WorkIteration(CancellationToken token)
    {
        _metrics.Begin();
        await _behavior.ExecuteAsync(token);
        _metrics.End();
        _tuner.ApplyAdjustments();
    }

    public void Stop()
    {
        Console.WriteLine("\n[BackgroundService] Сигнал остановки отправлен.");
        _cts.Cancel();
        _executionTask?.Wait(); // Ждем завершения текущего цикла
        Console.WriteLine("[BackgroundService] Сервис остановлен.");
    }

    public void Dispose() => _cts.Dispose();
}

#endregion

#region 4. Клиентское приложение (точка входа)

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Демонстрация композиции нескольких стратегий (Background Service)\n");

        // Компоновка объекта из готовых частей (Dependency Injection)
        using var service = new BackgroundService(
            scheduler: new IntervalScheduler(TimeSpan.FromSeconds(2)),
            behavior: new DataProcessingBehavior(workTimeMs: 1500), // ~1.5 секунды работы
            metrics: new TimingMetrics(),
            tuner: new PerformanceTuner(delayAdjustmentMs: 200)
        );

        service.Start();

        Console.WriteLine("Сервис работает. Нажмите любую клавишу для остановки...");
        Console.ReadKey(true); // Ждем нажатия клавиши без отображения символа

        service.Stop();
    }
}
#endregion