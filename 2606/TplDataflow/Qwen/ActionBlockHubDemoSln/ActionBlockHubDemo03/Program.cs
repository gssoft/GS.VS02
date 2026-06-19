// Program.cs

using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Options;
using ActionBlockHubDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading.Tasks.Dataflow;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // 1. Регистрируем настройки
    services.Configure<HubOptions>(hostContext.Configuration.GetSection(nameof(HubOptions)));

    services.AddHostedService<DataProcessingService>();
    services.AddHostedService<MetricsService>();

    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<DataHandler>();
        var dataHandler = new DataHandler(key, logger);
        return dataHandler.HandleAsync;
    });

    services.AddSingleton<IDeadLetterQueue<MyDataType>, InMemoryDeadLetterQueue<MyDataType>>();

    // 2. ActionBlockHub (читает ключи из конфига - у вас было верно)
    services.AddSingleton<IActionBlockHub<string, MyDataType>>(serviceProvider =>
    {
        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ActionBlockHub<string, MyDataType>>>();
        var dlq = serviceProvider.GetRequiredService<IDeadLetterQueue<MyDataType>>();

        var options = serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value;
        var blockSettings = options.ActionBlock;

        var blockOptions = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = blockSettings.BoundedCapacity,
            MaxDegreeOfParallelism = blockSettings.MaxDegreeOfParallelism
        };

        return new ActionBlockHub<string, MyDataType>(
            blockSettings.Keys,
            handlerFactory, logger, dlq, blockOptions);
    });

    // 3. ❗ ИСПРАВЛЕНИЕ: BroadcastHub тоже должен читать ключи из конфига!
    services.AddSingleton<IBroadcastHub<string, MyDataType>>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value;
        return new BroadcastHub<string, MyDataType>(options.ActionBlock.Keys);
    });
});

var host = builder.Build();

var actionHub = host.Services.GetRequiredService<IActionBlockHub<string, MyDataType>>();
var broadcastHub = host.Services.GetRequiredService<IBroadcastHub<string, MyDataType>>();

// 4. ❗ ИСПРАВЛЕНИЕ: Берем ключи из конфига для связывания, а не хардкодим!
var options = host.Services.GetRequiredService<IOptions<HubOptions>>().Value;
var keys = options.ActionBlock.Keys;

foreach (var key in keys)
{
    broadcastHub.LinkTo(key, actionHub.GetTargetBlock(key), new DataflowLinkOptions { PropagateCompletion = true });
}

await host.RunAsync();

//using ActionBlockHubDemo.Models;
//using ActionBlockHubDemo.Options;
//using ActionBlockHubDemo.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System.Text;
//using System.Threading.Tasks.Dataflow;

//Console.OutputEncoding = Encoding.UTF8;

//var builder = Host.CreateDefaultBuilder(args);

//builder.ConfigureServices((hostContext, services) =>
//{
//    // 1. Регистрируем настройки. Теперь их можно получить через IOptions<HubOptions>
//    services.Configure<HubOptions>(hostContext.Configuration.GetSection(nameof(HubOptions)));

//    services.AddHostedService<DataProcessingService>();

//    // РЕГИСТРИРУЕМ НАШ НОВЫЙ СЕРВИС МОНИТОРИНГА
//    services.AddHostedService<MetricsService>();

//    services.AddTransient<Func<string, Func<MyDataType, Task>>>(serviceProvider => key =>
//    {
//        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
//        var logger = loggerFactory.CreateLogger<DataHandler>();
//        var dataHandler = new DataHandler(key, logger);
//        return dataHandler.HandleAsync;
//    });


//    // РЕГИСТРИРУЕМ DEAD LETTER QUEUE КАК SINGLETON
//    services.AddSingleton<IDeadLetterQueue<MyDataType>, InMemoryDeadLetterQueue<MyDataType>>();

//    // РЕГИСТРИРУЕМ ИНТЕРФЕЙС IActionBlockHub
//    services.AddSingleton<IActionBlockHub<string, MyDataType>>(serviceProvider =>
//    {
//        var handlerFactory = serviceProvider.GetRequiredService<Func<string, Func<MyDataType, Task>>>();
//        // Получаем логгер для самого ActionBlockHub
//        var logger = serviceProvider.GetRequiredService<ILogger<ActionBlockHub<string, MyDataType>>>();
//        var dlq = serviceProvider.GetRequiredService<IDeadLetterQueue<MyDataType>>(); // <-- Получаем DLQ


//        var keys = new List<string> { "A", "B", "C" };
//       // var blockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = 1};

//        // ПОЛУЧАЕМ НАСТРОЙКИ ИЗ КОНФИГА
//        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HubOptions>>().Value;
//        var blockSettings = options.ActionBlock;

//        var blockOptions = new ExecutionDataflowBlockOptions
//        {
//            BoundedCapacity = blockSettings.BoundedCapacity,
//            MaxDegreeOfParallelism = blockSettings.MaxDegreeOfParallelism
//        };

//        return new ActionBlockHub<string, MyDataType>(
//            blockSettings.Keys, // Используем ключи из конфига
//            handlerFactory,
//            logger,
//            dlq,
//            blockOptions);


//        // return new ActionBlockHub<string, MyDataType>(keys, handlerFactory, logger, dlq, blockOptions);
//    });

//    // РЕГИСТРИРУЕМ ИНТЕРФЕЙС IBroadcastHub
//    services.AddSingleton<IBroadcastHub<string, MyDataType>>(sp =>
//        new BroadcastHub<string, MyDataType>(new List<string> { "A", "B", "C" }));
//});

//// ... логирование ...

//var host = builder.Build();  // !!!!! Ошибка

//// Теперь эта строка отработает успешно!
//var actionHub = host.Services.GetRequiredService<IActionBlockHub<string, MyDataType>>();
//var broadcastHub = host.Services.GetRequiredService<IBroadcastHub<string, MyDataType>>();

//// Связываем хабы
//foreach (var key in new[] { "A", "B", "C" })
//{
//    broadcastHub.LinkTo(key, actionHub.GetTargetBlock(key), new DataflowLinkOptions { PropagateCompletion = true });
//}

//await host.RunAsync();

