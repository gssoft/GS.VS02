// Program.cs

using Application.Commands;
using Application.Events;
using Application.Interfaces;
using Infrastructure.Buses;
using Infrastructure.Channels;
using Infrastructure.Routers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
// using WorkerServiceExample; // Для цвета в консоли

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureLogging(logging =>
{
    logging.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled; // Цветной вывод в консоли Windows Terminal/Powershell.
    });
});
builder.ConfigureServices((hostContext, services) =>
{
    // == 1. Регистрация каналов ==
    services.AddCommandChannel(); // Из Infrastructure.Channels.CommandChannel.AddCommandChannel()
    services.AddEventChannel();   // Из Infrastructure.Channels.EventChannel.AddEventChannel()

    // == 2. Регистрация шин ==
    services.AddSingleton<ICommandBus, CommandBus>();
    services.AddSingleton<IEventBus, EventBus>();

    // == 3. Регистрация роутеров как фоновых сервисов ==
    services.AddHostedService<CommandRouter>();
    services.AddHostedService<EventRouter>();

    // == 4. Регистрация бизнес-логики (хендлеров) ==

    // --- Команды ---
    services.AddTransient<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();

    // --- События ---
    services.AddTransient<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();

    // == 5. Регистрация воркера ==
    services.AddHostedService<Worker>();
});
// builder.UseWindowsService(); // Если планируется запуск как службы Windows

var host = builder.Build();
host.Run();



