using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#region 1. Интерфейсы стратегий
public interface IBehaviorStrategy { Task ExecuteAsync(CancellationToken ct); }
public interface ISchedulerStrategy { Task StartLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction); }
public interface IMetricsStrategy { void Begin(); void End(); }
public interface ITunerStrategy { void ApplyAdjustments(); }
#endregion

#region 2. Конкретные реализации
public class HeavyDbWork : IBehaviorStrategy
{
    public async Task ExecuteAsync(CancellationToken token)
    {
        Console.WriteLine("[Behavior] Тяжелая работа...");
        await Task.Delay(1000, token);
    }
}

public class FixedIntervalScheduler : ISchedulerStrategy
{
    private readonly TimeSpan _interval;
    public FixedIntervalScheduler(TimeSpan interval) => _interval = interval;

    public async Task StartLoopAsync(CancellationToken stoppingToken, Func<CancellationToken, Task> workAction)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await workAction(stoppingToken); await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

public class ConsoleLoggingMetrics : IMetricsStrategy
{
    private DateTime _start;
    public void Begin() => _start = DateTime.Now;
    public void End()
    {
        var elapsed = DateTime.Now - _start;
        Console.WriteLine($"[Metrics] Время: {elapsed.TotalMilliseconds} мс.");
    }
}

public class NoOpTuner : ITunerStrategy
{
    public void ApplyAdjustments() => Console.WriteLine("[Tuner] Настройки применены.");
}
#endregion

#region 3. Обобщенный BackgroundService
public class ComposedBackgroundService<TBehavior, TScheduler, TMetrics, TTuner> : BackgroundService
    where TBehavior : IBehaviorStrategy
    where TScheduler : ISchedulerStrategy
    where TMetrics : IMetricsStrategy
    where TTuner : ITunerStrategy
{
    private readonly TBehavior _behavior;
    private readonly TScheduler _scheduler;
    private readonly TMetrics _metrics;
    private readonly TTuner _tuner;

    public ComposedBackgroundService(
        TBehavior behavior,
        TScheduler scheduler,
        TMetrics metrics,
        TTuner tuner)
    {
        _behavior = behavior;
        _scheduler = scheduler;
        _metrics = metrics;
        _tuner = tuner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Service] Цикл запущен.");
        await _scheduler.StartLoopAsync(stoppingToken, async token =>
        {
            _metrics.Begin();
            try { await _behavior.ExecuteAsync(token); }
            finally { _metrics.End(); }
            _tuner.ApplyAdjustments();
        });
    }
}
#endregion

class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                /* ==========================================================
                   ===       КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ НИЖЕ                 ===
                   ========================================================== */

                // 1. Регистрируем конкретные классы стратегий.
                // Теперь Container знает, что такое "HeavyDbWork" и может его создать.
                services.AddSingleton<IBehaviorStrategy, HeavyDbWork>();
                services.AddSingleton<ISchedulerStrategy>(sp =>
                    new FixedIntervalScheduler(TimeSpan.FromSeconds(2)));
                services.AddSingleton<IMetricsStrategy, ConsoleLoggingMetrics>();
                services.AddSingleton<ITunerStrategy, NoOpTuner>();

                // 2. Регистрируем обобщенный хостинг-сервис.
                // Благодаря пункту №1, теперь при создании ComposedBackgroundService<...>
                // контейнер сможет найти все нужные типы (HeavyDbWork и др.).
                services.AddHostedService<
                    ComposedBackgroundService<
                        IBehaviorStrategy,          // Используем ИНТЕРФЕЙСЫ здесь!
                        ISchedulerStrategy,
                        IMetricsStrategy,
                        ITunerStrategy>>();
            })
            .Build();

        await host.RunAsync();
    }
}

