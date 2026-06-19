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

    // РЕГИСТРИРУЕМ НАШ НОВЫЙ СЕРВИС МОНИТОРИНГА
    services.AddHostedService<MetricsService>();

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
        // Получаем логгер для самого ActionBlockHub
        var logger = serviceProvider.GetRequiredService<ILogger<ActionBlockHub<string, MyDataType>>>();

        var keys = new List<string> { "A", "B", "C" };
        var blockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = 1 };

        return new ActionBlockHub<string, MyDataType>(keys, handlerFactory, logger, blockOptions);
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

