using Worker.Core.Abstractions;
using Worker.Core.Infrastructure;
using Worker.Core.Infrastructure.Clocks;
// using Worker.Core.Infrastructure.Hosting;
using Worker.Core.Infrastructure.Liveness;
using Worker.Core.Infrastructure.Schedulers;
using WorkerApp.Behaviors;

var builder = Host.CreateApplicationBuilder(args);

// Регистрация инфраструктуры (можно вынести в Extension Method AddWorkerCore())
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ILivenessStrategy, DefaultLivenessStrategy>();

// Конфигурация планировщика через appsettings.json или жестко здесь
builder.Services.AddSingleton<ISchedulerStrategy>(sp =>
    new ExponentialBackoffScheduler(
        sp.GetRequiredService<IClock>(),
        baseDelay: TimeSpan.FromSeconds(10),
        maxDelay: TimeSpan.FromMinutes(5)));

// Регистрация конкретного поведения
builder.Services.AddTransient<IBehaviorStrategy, DatabasePollingBehavior>();

// Вместо стандартного worker'а запускаем наш универсальный хост
builder.Services.AddHostedService<GenericJobHost>();

var host = builder.Build();
await host.RunAsync();
