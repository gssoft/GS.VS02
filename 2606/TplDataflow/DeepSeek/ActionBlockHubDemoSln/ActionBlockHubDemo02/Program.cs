// Program.cs

using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

var builder = Host.CreateDefaultBuilder(args);



builder.ConfigureServices((hostContext, services) =>
{
    // 1. Регистрируем наш сервис
    services.AddHostedService<DataProcessingService>();

    // 2. РЕГИСТРИРУЕМ ФАБРИКУ ДЛЯ DATAHANDLER (ПРАВИЛЬНОЕ РЕШЕНИЕ)
    // Мы больше не полагаемся на контейнер для создания DataHandler.
    // Мы создаем его вручную, передавая ключ и получая ILogger через фабрику.
    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
    {
        // Получаем ILogger через фабрику. Это безопасно.
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<DataHandler>();

        // СОЗДАЕМ DataHandler ВРУЧНУЮ, передавая ключ (handlerName) и логгер.
        var dataHandler = new DataHandler(key, logger);

        // Возвращаем его метод HandleAsync
        return dataHandler.HandleAsync;
    });

    // 3. Регистрируем наш ActionBlockHub как Singleton.
    services.AddSingleton<ActionBlockHub<string, MyDataType>>(serviceProvider =>
    {
        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();
        var keys = new List<string> { "A", "B", "C" };
        var blockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = 1 };

        return new ActionBlockHub<string, MyDataType>(keys, handlerFactory, blockOptions);
    });

    // Регистрируем наш новый BroadcastHub как Singleton
    services.AddSingleton<BroadcastHub<string, MyDataType>>(sp =>
        new BroadcastHub<string, MyDataType>(new List<string> { "A", "B", "C" }));
});

builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

var host = builder.Build();
await host.RunAsync();

