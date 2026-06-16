// Program.cs

using EventBus.Abstractions;
using EventBus.Extensions;
using EventHubWorkerService.Handlers;
using EventHubWorkerService.Messages;
using EventHubWorkerService.Services;
using Microsoft.Extensions.Hosting;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

// 1. Регистрируем шину событий
builder.Services.AddChannelEventBus();

// 2. Регистрируем обработчик (транзиентный, создаётся на каждое событие)
builder.Services.AddTransient<IHandler<MyEvent>, MyEventHandler>();

// 3. Регистрируем подписчика с буфером на 500 сообщений
// AddSubscriber автоматически регистрирует и сам сервис, и его канал
builder.Services.AddSubscriber<MyEvent, MyAnalyticsService>(bufferSize: 500);

// 4. Регистрируем паблишер
builder.Services.AddHostedService<DataProducerService>();

await builder.Build().RunAsync();

//using EventHubWorkerService;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
