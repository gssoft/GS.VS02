using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using DS.BackgroundServices.Core02;

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
