using DS.BackgroundServices.Core.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Регистрация фоновых сервисов (порядок не важен)
builder.Services.AddHostedService<PeriodicProcessor>();
builder.Services.AddHostedService<ContinuousListener>();
builder.Services.AddHostedService<StartupInitializer>();
builder.Services.AddHostedService<CronJob>();

// При необходимости можно зарегистрировать дополнительные зависимости
// builder.Services.AddSingleton<IMyService, MyService>();

var host = builder.Build();

Console.WriteLine("Запуск хоста...");
await host.RunAsync();

// ------------------------------------------------------------
// Конкретные реализации фоновых сервисов (можно вынести в отдельные файлы)
// ------------------------------------------------------------

/// <summary>Периодическая обработка каждые 5 секунд.</summary>
public class PeriodicProcessor : PeriodicBackgroundService
{
    private readonly ILogger<PeriodicProcessor> _logger;

    public PeriodicProcessor(ILogger<PeriodicProcessor> logger)
        : base(logger, TimeSpan.FromSeconds(5)) {
        _logger = logger;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeriodicProcessor: выполняю работу...");
        await Task.Delay(200, stoppingToken); // имитация полезной нагрузки
    }
}

/// <summary>Непрерывный конвейер (слушает очередь/канал).</summary>
public class ContinuousListener : ContinuousBackgroundService
{
    private readonly ILogger<ContinuousListener> _logger;
    public ContinuousListener(ILogger<ContinuousListener> logger) : base(logger) {
        _logger = logger;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContinuousListener: ожидаю новые данные...");
        // В реальном коде здесь ожидание сообщения из канала/сокета
        await Task.Delay(2000, stoppingToken);
    }
}

/// <summary>Одноразовое задание при запуске.</summary>
public class StartupInitializer : OneTimeStartupService
{
    private readonly ILogger<StartupInitializer> _logger;
    public StartupInitializer(ILogger<StartupInitializer> logger) : base(logger) {
        _logger = logger;
    }

    protected override async Task ExecuteOnceAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StartupInitializer: выполняю разовую инициализацию...");
        await Task.Delay(1000, stoppingToken);
        _logger.LogInformation("StartupInitializer: инициализация завершена.");
    }
}

/// <summary>Задача по Cron‑расписанию (каждые 10 секунд).</summary>
public class CronJob : ScheduledBackgroundService
{
    private readonly ILogger<CronJob> _logger;
    public CronJob(ILogger<CronJob> logger)
        : base(logger, "*/10 * * * * *") {
        _logger = logger;
    
    } // Cronos формат с секундами

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CronJob: запуск по расписанию в {Time}", DateTime.UtcNow);
        await Task.Delay(300, stoppingToken);
    }
}

// ------------------------------------------------------------
// Точка входа
// ------------------------------------------------------------
//HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//// Регистрация фоновых сервисов (порядок не важен)
//builder.Services.AddHostedService<PeriodicProcessor>();
//builder.Services.AddHostedService<ContinuousListener>();
//builder.Services.AddHostedService<StartupInitializer>();
//builder.Services.AddHostedService<CronJob>();

//// При необходимости можно зарегистрировать дополнительные зависимости
//// builder.Services.AddSingleton<IMyService, MyService>();

//var host = builder.Build();

//Console.WriteLine("Запуск хоста...");
//await host.RunAsync();
