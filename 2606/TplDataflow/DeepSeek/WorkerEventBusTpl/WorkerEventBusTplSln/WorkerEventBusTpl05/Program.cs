// Версия интенсивная с расширенным мониторингом
// Program.cs

using WorkerEventBus;
using WorkerEventBus.Handlers;

var builder = Host.CreateApplicationBuilder(args);

// Настройка логирования для цветного вывода в консоль
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "simple";
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});

// Регистрируем обработчики
builder.Services.AddSingleton<HandlerA>();
builder.Services.AddSingleton<HandlerB>();
builder.Services.AddSingleton<HandlerC>();

// Регистрируем EventBus как Singleton
builder.Services.AddSingleton<EventBus>();

// Регистрируем Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Настройка цветов консоли для лучшего визуального восприятия
Console.Title = "EventBus with TPL Dataflow - Enhanced Monitoring";
Console.BackgroundColor = ConsoleColor.Black;
Console.ForegroundColor = ConsoleColor.White;

// Вывод приветственного баннера
Console.WriteLine();
Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    EVENT BUS WITH TPL DATAFLOAD                            ║");
Console.WriteLine("║                         ENHANCED MONITORING MODE                           ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

await host.RunAsync();
