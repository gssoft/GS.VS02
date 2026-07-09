using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Модель для передачи параметров в стратегию тюнинга
public record TunerConfig(double Gain, int Offset);

class Program
{
    static async Task Main(string[] args)
    {
        // Создаем хост. Он обеспечит нам DI-контейнер и корректную остановку по Ctrl+C.
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                #region РЕГИСТРАЦИЯ СТРАТЕГИЙ-ДЕЛЕГАТОВ

                // 1. Стратегия поведения: что делаем? 
                // Асинхронная функция, принимающая токен отмены.
                services.AddSingleton<Func<CancellationToken, Task>>(sp => async token =>
                {
                    Console.WriteLine("[Behavior] Выполняем тяжелую работу...");
                    await Task.Delay(1200, token); // Симуляция работы БД или сети
                    Console.WriteLine("[Behavior] Работа завершена.");
                });

                // 2. Стратегия планирования: когда запускаем?
                // Функция принимает интервал, токен и само действие (work), которое нужно выполнить.
                services.AddSingleton<Func<TimeSpan, CancellationToken, Func<CancellationToken, Task>, Task>>(
                    sp => async (interval, stoppingToken, workAction) =>
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"[Scheduler] Планирую задачу (интервал {interval.TotalSeconds} сек)...");
                            await workAction(stoppingToken);

                            try
                            {
                                await Task.Delay(interval, stoppingToken);
                            }
                            catch (TaskCanceledException) { /* Игнорируем при остановке */ }
                        }
                    });

                // 3. Стратегия метрик: как измеряем?
                // Просто Action (void), который пишет в лог/консоль.
                services.AddSingleton<Action<string, double>>(sp => (stage, value) =>
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Console.WriteLine($"[Metrics] [{timestamp}] Этап: '{stage}', Значение: {value}");
                });

                // 4. Стратегия тюнинга: какие параметры применяем?
                // Чистая функция (Func), которая берет конфиг и возвращает измененный конфиг.
                services.AddSingleton<Func<TunerConfig, TunerConfig>>(sp => config =>
                {
                    Console.WriteLine($"[Tuner] Применена поправка: Gain x{config.Gain}, Offset +{config.Offset}");

                    // Возвращаем новый объект с измененными параметрами (иммутабельность)
                    return config with { Gain = config.Gain * 0.95 };
                });

                #endregion

                // Регистрируем наш рабочий сервис
                services.AddHostedService<MyDelegatingWorker>();
            })
            .Build();

        await host.RunAsync(); // Запускаем приложение
    }
}

/// <summary>
/// Контекст, который объединяет все делегаты-стратегии.
/// </summary>
public class MyDelegatingWorker : BackgroundService
{
    private readonly Func<CancellationToken, Task> _behavior;
    private readonly Func<TimeSpan, CancellationToken, Func<CancellationToken, Task>, Task> _scheduler;
    private readonly Action<string, double> _metrics;
    private readonly Func<TunerConfig, TunerConfig> _tuner;

    public MyDelegatingWorker(
        Func<CancellationToken, Task> behavior,
        Func<TimeSpan, CancellationToken, Func<CancellationToken, Task>, Task> scheduler,
        Action<string, double> metrics,
        Func<TunerConfig, TunerConfig> tuner)
    {
        _behavior = behavior;
        _scheduler = scheduler;
        _metrics = metrics;
        _tuner = tuner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[MyDelegatingWorker] Сервис запущен.\n");

        // Начальные параметры для тюнера
        var initialConfig = new TunerConfig(Gain: 1.0, Offset: 100);

        // Вызываем планировщик. Обратите внимание, как мы передаем "тело" задачи.
        await _scheduler(TimeSpan.FromSeconds(2), stoppingToken, async token =>
        {
            // --- Начало одной итерации работы ---

            _metrics.Invoke("Start", 0); // Логируем начало

            await _behavior(token);      // Выполняем основную логику

            // Пример использования стратегии тюнинга внутри цикла
            var tunedConfig = _tuner(initialConfig);

            _metrics.Invoke("End", tunedConfig.Offset); // Логируем конец с данными из тюнера

            // --- Конец итерации ---
        });

        Console.WriteLine("\n[MyDelegatingWorker] Цикл планировщика завершен.");
    }
}

//using WorkerService07;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
