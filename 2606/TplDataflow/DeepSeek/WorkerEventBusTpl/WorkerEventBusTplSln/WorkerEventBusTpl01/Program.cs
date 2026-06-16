using WorkerEventBus;
using WorkerEventBus.Handlers;

var builder = Host.CreateApplicationBuilder(args);

// Регистрируем обработчики
builder.Services.AddSingleton<HandlerA>();
builder.Services.AddSingleton<HandlerB>();
builder.Services.AddSingleton<HandlerC>();

// Регистрируем EventBus как Singleton
builder.Services.AddSingleton<EventBus>();

// Регистрируем Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

//using WorkerEventBusTpl01;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
