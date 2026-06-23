////// Program.cs
/////
//// Program.cs или Startup.cs

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting; // Обязательно добавить этот using

//var builder = WebApplication.CreateBuilder(args);

//// --- Регистрация компонентов ---
//// Глобальный хаб - синглтон, чтобы все сервисы работали с одним экземпляром.
//builder.Services.AddSingleton<GlobalEventHub>();
//// Микро-шина - синглтон на время жизни сервиса (Scoped не подойдет для BackgroundService).
//builder.Services.AddSingleton<MicroEventBus>();
//// Логгеры по умолчанию

//// --- Регистрация сервисов ---
//builder.Services.AddHostedService<Service1>();
//builder.Services.AddHostedService<Service2>();
//// ... AddHostedService<Service3>(); и т.д.


//var app = builder.Build();
//app.Run();

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // Обязательно добавить этот using

public class Program
{
    public static void Main(string[] args)
    {
        // Создаем стандартный хост вместо WebApplication
        var builder = Host.CreateDefaultBuilder(args);

        // --- Регистрация компонентов ---
        // Глобальный хаб - синглтон, чтобы все сервисы работали с одним экземпляром.
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<GlobalEventHub>();
            services.AddSingleton<MicroEventBus>();

            // Логгеры по умолчанию добавляются CreateDefaultBuilder,
            // но если нужны дополнительные настройки, их можно сделать здесь.

            // --- Регистрация сервисов ---
            services.AddHostedService<Service1>();
            services.AddHostedService<Service2>();
            // ... AddHostedService<Service3>(); и т.д.
        });

        // Запускаем созданный хост
        var host = builder.Build();
        host.Run();
    }
}
