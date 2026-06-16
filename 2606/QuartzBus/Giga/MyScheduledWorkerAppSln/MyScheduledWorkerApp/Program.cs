using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

// --- 1. Определение нашей задачи (Job) для Quartz ---
// Эта задача будет выполняться по расписанию.
public class ScheduledTask : IJob
{
    private readonly ILogger<ScheduledTask> _logger;

    public ScheduledTask(ILogger<ScheduledTask> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        // Эта логика выполнится по расписанию (каждые 15 секунд)
        _logger.LogInformation("ScheduledTask is running at {time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}

// 2.Стандартный Worker(оставляем как есть)-- -
// Он будет работать параллельно с Quartz.
public class Worker(ILogger<Worker> logger) : BackgroundService
{
    // Добавляем поле для флага остановки
    private bool _isStopping = false;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started.");

        // Подписываемся на сигнал отмены.
        // Когда придет сигнал, мы установим наш флаг в true,
        // но не будем немедленно прерывать цикл.
        stoppingToken.Register(() =>
        {
            _logger.LogInformation("Worker received shutdown signal. Initiating graceful shutdown...");
            _isStopping = true;
        });

        // Цикл продолжается, пока не получен сигнал ИЛИ пока мы сами не решим остановиться.
        while (!_isStopping)
        {
            try
            {
                // Эмулируем какую-то работу.
                // В реальном приложении здесь может быть более сложная логика.
                _logger.LogInformation("Worker is doing some work at: {time}", DateTimeOffset.Now);

                // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Убираем stoppingToken из Task.Delay.
                // Это позволит задержке завершиться, даже если пришел сигнал на остановку.
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                // Логируем непредвиденные ошибки, чтобы сервис не "упал".
                _logger.LogError(ex, "An error occurred during worker execution.");
            }
        }

        // Этот код выполнится после того, как цикл while завершится.
        // Здесь можно разместить финальную очистку ресурсов.
        _logger.LogInformation("Worker is performing final cleanup and shutting down gracefully...");

        // Например, если бы у нас был открытый ресурс:
        // await _myResource.CloseAsync();
    }
}

    //public class Worker : BackgroundService
    //{
    //    private readonly ILogger<Worker> _logger;

    //    public Worker(ILogger<Worker> logger)
    //    {
    //        _logger = logger;
    //    }

    //    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //    {
    //        while (!stoppingToken.IsCancellationRequested)
    //        {
    //            // Этот лог будет появляться каждую секунду.
    //            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
    //            await Task.Delay(1000, stoppingToken);
    //        }
    //    }
    //}





    // --- 3. Конфигурация и запуск приложения ---
    public class Program
    {
    [Obsolete]
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }
    [Obsolete]
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Регистрируем наш стандартный Worker.
                // Это создаст один IHostedService.
                services.AddHostedService<Worker>();

                // --- НАЧАЛО БЛОКА QUARTZ ---
                // Регистрируем саму библиотеку Quartz.
                _ = services.AddQuartz(q =>
                {
                    // Используем DI для создания экземпляров задач.
                    q.UseMicrosoftDependencyInjectionJobFactory();

                    // Создаем JobKey - уникальный идентификатор нашей задачи.
                    var jobKey = new JobKey("scheduledTaskJob");

                    // Регистрируем задачу (Job) с нашим ключом.
                    q.AddJob<ScheduledTask>(opts => opts.WithIdentity(jobKey));

                    // Создаем триггер (Trigger) для этой задачи.
                    // Запускать каждые 15 секунд для наглядности.
                    q.AddTrigger(opts => opts
                        .ForJob(jobKey) // Привязываем к нашей задаче
                        .WithIdentity("simpleTrigger")
                        .WithSimpleSchedule(x => x
                            .WithInterval(TimeSpan.FromSeconds(15))
                            .RepeatForever()));
                });

                // Эта строка регистрирует Quartz как IHostedService.
                // Это создаст ВТОРОЙ IHostedService, который будет работать параллельно с Worker.
                services.AddQuartzHostedService();
                // --- КОНЕЦ БЛОКА QUARTZ ---
            });
}

//using MyScheduledWorkerApp;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
