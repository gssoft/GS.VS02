// Program.cs

using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // 1. Регистрируем наш сервис
    services.AddHostedService<DataProcessingService>();

    // 2. РЕГИСТРИРУЕМ ФАБРИКУ ДЛЯ DATAHANDLER ПО-НОВОМУ
    // Теперь мы регистрируем Func<string, DataHandler>.
    // DI-контейнер будет знать, что для создания DataHandler ему нужна строка.
    services.AddTransient<Func<string, DataHandler>>(serviceProvider => key =>
    {
        // Создаем scope для получения зависимостей (например, ILogger)
        var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataHandler>>();

        // Создаем и возвращаем экземпляр DataHandler с нужным именем (key)
        return new DataHandler(key, logger);
    });

    // 3. Регистрируем сам DataHandler как Transient (теперь это делает фабрика)
    // services.AddTransient<DataHandler>(); // Эта строка больше НЕ нужна

    // 4. Регистрируем наш ActionBlockHub как Singleton
    services.AddSingleton<ActionBlockHub<string, MyDataType>>(serviceProvider =>
    {
        // Получаем фабрику из DI-контейнера
        var handlerFactory = serviceProvider.GetRequiredService<Func<string, DataHandler>>();

        var keys = new List<string> { "A", "B", "C" };

        return new ActionBlockHub<string, MyDataType>(keys, key =>
        {
            // Здесь мы используем фабрику для создания хэндлера по ключу
            var handlerInstance = handlerFactory(key);

            // Возвращаем делегат HandleAsync, который будет выполнять блок
            return handlerInstance.HandleAsync;
        });
    });
});

builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

var host = builder.Build();
await host.RunAsync();


//using ActionBlockHubDemo.Models;
//using ActionBlockHubDemo.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateDefaultBuilder(args);

//builder.ConfigureServices((hostContext, services) =>
//{
//    // Регистрируем наш Worker-сервис (можно использовать и BackgroundService)
//    services.AddHostedService<DataProcessingService>();

//    // Регистрируем фабрику для создания хэндлеров.
//    // Это позволит создавать отдельный экземпляр DataHandler для каждого ключа.
//    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
//    {
//        // Создаем новый scope, чтобы получить свой экземпляр DataHandler с логгером.
//        var scope = serviceProvider.CreateScope();

//        // Имя хэндлера будет соответствовать ключу (A, B или C)
//        var handlerInstance = scope.ServiceProvider.GetRequiredService<DataHandler>();

//        return async (data) =>
//        {
//            try
//            {
//                await handlerInstance.HandleAsync(data);
//            }
//            finally
//            {
//                // Важно! Dispose scope после использования.
//                scope.Dispose();
//            }
//        };
//    });

//    // Регистрируем сам DataHandler как Transient, чтобы каждый раз получать новый экземпляр.
//    services.AddTransient<DataHandler>();

//    // Регистрируем наш ActionBlockHub как Singleton.
//    // При создании он получит список ключей и фабрику хэндлеров.
//    services.AddSingleton<ActionBlockHub<string, MyDataType>>(serviceProvider =>
//    {
//        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();

//        // Здесь мы определяем наши "каналы"
//        var keys = new List<string> { "A", "B", "C" };

//        return new ActionBlockHub<string, MyDataType>(keys, handlerFactory);
//    });
//});

//builder.ConfigureLogging(logging =>
//{
//    logging.ClearProviders();
//    logging.AddConsole(); // Добавляем консольный логгер для наглядности
//});

//var host = builder.Build();
//await host.RunAsync();
