// BusMicro.ConsoleApp/Program.cs
using BusMicro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;

// --- 1. Определяем наши сообщения ---
public record UserCreatedEvent(string Username, string Email) : IMessage;
public record SendWelcomeEmailCommand(Guid UserId) : BusMicro.ICommand;

// --- 2. Реализуем обработчики ---
public class UserCreatedLogger : IHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedLogger> _logger;

    public UserCreatedLogger(ILogger<UserCreatedLogger> logger) => _logger = logger;

    public Task HandleAsync(UserCreatedEvent message, CancellationToken ct)
    {
        _logger.LogInformation($"Пользователь создан: {message.Username} ({message.Email})");
        return Task.CompletedTask;
    }
}

public class WelcomeEmailSender : IHandler<SendWelcomeEmailCommand>
{
    private readonly ILogger<WelcomeEmailSender> _logger;

    public WelcomeEmailSender(ILogger<WelcomeEmailSender> logger) => _logger = logger;

    public Task HandleAsync(SendWelcomeEmailCommand message, CancellationToken ct)
    {
        _logger.LogInformation($"Отправка приветственного письма пользователю с ID: {message.UserId}");
        // Имитация отправки email
        return Task.Delay(500, ct);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                // 1. Сначала СОЗДАЕМ канал
                var channel = Channel.CreateUnbounded<IMessage>();

                // 2. Затем регистрируем шину, ПЕРЕДАВАЯ в нее созданный канал
                services.AddBusMicro(channel);

                // Регистрируем конкретные обработчики
                services.AddTransient<IHandler<UserCreatedEvent>, UserCreatedLogger>();
                services.AddTransient<IHandler<SendWelcomeEmailCommand>, WelcomeEmailSender>();
            })
            .Build();

        await host.StartAsync();

        var bus = host.Services.GetRequiredService<IMessageBus>();

        await bus.SendAsync(new SendWelcomeEmailCommand(Guid.NewGuid()));
        await bus.PublishAsync(new UserCreatedEvent("IvanovII", "ivanov@domain.com"));

        Console.WriteLine("Сообщения отправлены. Нажмите Enter для выхода...");
        Console.ReadLine();

        await host.StopAsync();
    }
}

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        var host = Host.CreateDefaultBuilder(args)
//            .ConfigureLogging(logging =>
//            {
//                logging.ClearProviders(); // Убираем стандартный вывод
//                logging.AddConsole();     // Добавляем свой, более красивый
//            })
//            .ConfigureServices((context, services) =>
//            {
//                // Регистрируем нашу микро-шину
//                services.AddBusMicro();

//                // Регистрируем конкретные обработчики
//                services.AddTransient<IHandler<UserCreatedEvent>, UserCreatedLogger>();
//                services.AddTransient<IHandler<SendWelcomeEmailCommand>, WelcomeEmailSender>();
//            })
//            .Build();

//        // Запускаем хост, чтобы запустился фоновый сервис MessageRouter
//        await host.StartAsync();

//        // Получаем экземпляр шины из DI-контейнера
//        var bus = host.Services.GetRequiredService<IMessageBus>();

//        // Отправляем команду и публикуем событие
//        await bus.SendAsync(new SendWelcomeEmailCommand(Guid.NewGuid()));
//        await bus.PublishAsync(new UserCreatedEvent("IvanovII", "ivanov@domain.com"));

//        Console.WriteLine("Сообщения отправлены. Нажмите Enter для выхода...");
//        Console.ReadLine();

//        // Корректная остановка приложения
//        await host.StopAsync();
//    }
//}
