using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#region 1. Интерфейсы стратегий (остаются прежними)
public interface IBehaviorStrategy { Task ExecuteAsync(CancellationToken ct); }
public interface ISchedulerStrategy { Task ScheduleLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction); }
public interface IMetricsStrategy { void Begin(); void End(); }
public interface ITunerStrategy { void ApplyAdjustments(); }
#endregion

#region 2. Конкретные реализации стратегий (остаются прежними)
public class DataProcessingBehavior : IBehaviorStrategy
{
    private readonly int _workTimeMs;
    public DataProcessingBehavior(int workTimeMs) => _workTimeMs = workTimeMs;
    public async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine($"[Behavior] Работа ({_workTimeMs} мс)...");

        try
        {
            await Task.Delay(_workTimeMs, ct);
        }
        catch (OperationCanceledException)
        {
            // _logger.LogInformation("👋 Worker stopping due to cancellation");
            Console.WriteLine("👋 Worker stopping due to cancellation");
        }
    }
}
public class IntervalScheduler : ISchedulerStrategy
{
    private readonly TimeSpan _interval;
    public IntervalScheduler(TimeSpan interval) => _interval = interval;
    public async Task ScheduleLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await workAction(stoppingToken);
            try { await Task.Delay(_interval, stoppingToken); } catch (TaskCanceledException) { /* Игнорируем */ }
        }
    }
}
public class TimingMetrics : IMetricsStrategy
{
    private DateTimeOffset _start;
    public void Begin() => _start = DateTimeOffset.Now;
    public void End() => Console.WriteLine($"[Metrics] Прошло: {(DateTimeOffset.Now - _start).TotalMilliseconds:F0} мс.");
}
public class PerformanceTuner : ITunerStrategy
{
    private readonly int _adj;
    public PerformanceTuner(int adj) => _adj = adj;
    public void ApplyAdjustments() => Console.WriteLine($"[Tuner] Юстировка применена (-{_adj} мс).");
}
#endregion

// 3. Наш рабочий сервис НАСЛЕДУЕТ BackgroundService 
// Но теперь он получает зависимости через конструктор DI-контейнера
// Это номер 2 - опасаемся, что не зарегистрируем что-то
public class MyWorker : BackgroundService
{
    private readonly ISchedulerStrategy _scheduler;
    private readonly IBehaviorStrategy _behavior;
    private readonly IMetricsStrategy _metrics;
    private readonly ITunerStrategy _tuner;

    public MyWorker(ISchedulerStrategy scheduler, IBehaviorStrategy behavior,
                    IMetricsStrategy metrics, ITunerStrategy tuner)
    {
        _scheduler = scheduler;
        _behavior = behavior;
        _metrics = metrics;
        _tuner = tuner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[MyWorker] Фоновый цикл запущен. Нажмите Ctrl+C для выхода.");

        // Здесь мы используем композицию стратегий из предыдущего примера
        await _scheduler.ScheduleLoopAsync(stoppingToken, DoWorkIteration);
    }

    private async Task DoWorkIteration(CancellationToken token)
    {
        _metrics.Begin();
        await _behavior.ExecuteAsync(token);
        _metrics.End();
        _tuner.ApplyAdjustments();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        // Создание "Хоста" (Host) — это сердце современного .NET приложения
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // РЕГИСТРАЦИЯ СТРАТЕГИЙ В КОНТЕЙНЕРЕ (Dependency Injection)

                // Регистрируем наш воркер как Hosted Service
                services.AddHostedService<MyWorker>();

                // Регистрируем конкретные реализации стратегий
                services.AddSingleton<ISchedulerStrategy>(sp => new IntervalScheduler(TimeSpan.FromSeconds(2)));
                services.AddSingleton<IBehaviorStrategy>(sp => new DataProcessingBehavior(1500));
                services.AddSingleton<IMetricsStrategy, TimingMetrics>();
                services.AddSingleton<ITunerStrategy>(sp => new PerformanceTuner(200));
            })
            .Build();

        // Запуск хоста. Он сам создаст MyWorker, найдет там BackgroundService,
        // вызовет ExecuteAsync и будет ждать нажатия Ctrl+C.
        await host.RunAsync();
    }
}


//using WorkerService05;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
