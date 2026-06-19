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
    services.AddHostedService<DataProcessingService>();

    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<DataHandler>();
        var dataHandler = new DataHandler(key, logger);
        return dataHandler.HandleAsync;
    });

    // РЕГИСТРИРУЕМ ИНТЕРФЕЙС IActionBlockHub
    services.AddSingleton<IActionBlockHub<string, MyDataType>>(serviceProvider =>
    {
        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();
        var keys = new List<string> { "A", "B", "C" };
        var blockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = 1 };

        return new ActionBlockHub<string, MyDataType>(keys, handlerFactory, blockOptions);
    });

    // РЕГИСТРИРУЕМ ИНТЕРФЕЙС IBroadcastHub
    services.AddSingleton<IBroadcastHub<string, MyDataType>>(sp =>
        new BroadcastHub<string, MyDataType>(new List<string> { "A", "B", "C" }));
});

// ... логирование ...

var host = builder.Build();  // !!!!! Ошибка

// Теперь эта строка отработает успешно!
var actionHub = host.Services.GetRequiredService<IActionBlockHub<string, MyDataType>>();
var broadcastHub = host.Services.GetRequiredService<IBroadcastHub<string, MyDataType>>();

// Связываем хабы
foreach (var key in new[] { "A", "B", "C" })
{
    broadcastHub.LinkTo(key, actionHub.GetTargetBlock(key), new DataflowLinkOptions { PropagateCompletion = true });
}

await host.RunAsync();

//builder.ConfigureServices((hostContext, services) =>
//{
//    // 1. Регистрируем наш сервис
//    services.AddHostedService<DataProcessingService>();

//    // 2. РЕГИСТРИРУЕМ ФАБРИКУ ДЛЯ DATAHANDLER (ПРАВИЛЬНОЕ РЕШЕНИЕ)
//    // Мы больше не полагаемся на контейнер для создания DataHandler.
//    // Мы создаем его вручную, передавая ключ и получая ILogger через фабрику.
//    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
//    {
//        // Получаем ILogger через фабрику. Это безопасно.
//        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
//        var logger = loggerFactory.CreateLogger<DataHandler>();

//        // СОЗДАЕМ DataHandler ВРУЧНУЮ, передавая ключ (handlerName) и логгер.
//        var dataHandler = new DataHandler(key, logger);

//        // Возвращаем его метод HandleAsync
//        return dataHandler.HandleAsync;
//    });

//    // 3. Регистрируем наш ActionBlockHub как Singleton.
//    services.AddSingleton<ActionBlockHub<string, MyDataType>>(serviceProvider =>
//    {
//        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();
//        var keys = new List<string> { "A", "B", "C" };
//        var blockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = 1 };

//        return new ActionBlockHub<string, MyDataType>(keys, handlerFactory, blockOptions);
//    });

//    // Регистрируем наш новый BroadcastHub как Singleton
//    services.AddSingleton<BroadcastHub<string, MyDataType>>(sp =>
//        new BroadcastHub<string, MyDataType>(new List<string> { "A", "B", "C" }));
//});

//builder.ConfigureLogging(logging =>
//{
//    logging.ClearProviders();
//    logging.AddConsole();
//});

//var host = builder.Build();

//// 26.06.19
//// Получаем хабы из DI
//var actionHub = host.Services.GetRequiredService<IActionBlockHub<string, MyDataType>>();  // !!!! Ошибка
//var broadcastHub = host.Services.GetRequiredService<IBroadcastHub<string, MyDataType>>();

//// Связываем BroadcastBlock с ActionBlock для каждого ключа
//// PropagateCompletion = true критически важен для корректного завершения (Graceful Shutdown)
//foreach (var key in new[] { "A", "B", "C" })
//{
//    broadcastHub.LinkTo(key, actionHub.GetTargetBlock(key), new DataflowLinkOptions { PropagateCompletion = true });
//}

//await host.RunAsync();
