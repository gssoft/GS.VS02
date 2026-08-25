using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.App;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
builder.Services.AddSingleton<InMemoryDatabase>();

builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
{
    var bus = sp.GetRequiredService<IMicroEventBus>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };
    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
});

builder.Services.AddSingleton<StrategyProcessor>();
builder.Services.AddSingleton<OrderExecutorProcessor>();
builder.Services.AddSingleton<TradeProcessor>();
builder.Services.AddSingleton<PositionProcessor>();
builder.Services.AddSingleton<PortfolioProcessor>();

builder.Services.AddHostedService<TradingWorker>();

var host = builder.Build();

// Принудительно создаём все процессоры, чтобы они подписались на события
host.Services.GetRequiredService<StrategyProcessor>();
host.Services.GetRequiredService<OrderExecutorProcessor>();
host.Services.GetRequiredService<TradeProcessor>();
host.Services.GetRequiredService<PositionProcessor>();
host.Services.GetRequiredService<PortfolioProcessor>();

await host.RunAsync();

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//// Шина
//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();

//// Хранилище
//builder.Services.AddSingleton<InMemoryDatabase>();

//// Регистрация QuotesFeederProcessor с явным указанием тикеров
//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };  // можно загружать из конфигурации
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//// Остальные процессоры
//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//// Worker
//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();


//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//var builder = Host.CreateApplicationBuilder(args);

//// Шина
//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();

//// Хранилище
//builder.Services.AddSingleton<InMemoryDatabase>();

//// Процессоры (singleton, чтобы жили всё время)
//builder.Services.AddSingleton<QuotesFeederProcessor>();
//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//// Worker
//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();
