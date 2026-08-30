//// 26.08.30
///

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.App;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Создаём логгер вручную (или можно через DI, но проще для загрузчика)
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);

// Загружаем манифест
var manifestPath = builder.Configuration["ManifestPath"] ?? "manifest.json";
var manifest = ManifestLoader.Load(manifestPath);

// Создаём общие компоненты
var bus = new InMemoryMicroEventBus();
var db = new InMemoryDatabase();

// Создаём процессоры из манифеста
var processors = ManifestLoader.CreateProcessors(manifest, bus, loggerFactory, db);

// Регистрируем в DI
builder.Services.AddSingleton<IMicroEventBus>(bus);
builder.Services.AddSingleton<InMemoryDatabase>(db);

foreach (var processor in processors)
{
    builder.Services.AddSingleton(processor.GetType(), processor);
}

// Регистрируем TradingWorker
builder.Services.AddHostedService<TradingWorker>();

var host = builder.Build();
await host.RunAsync();


//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();

//var configuration = builder.Configuration;
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
//builder.Services.AddSingleton<InMemoryDatabase>();

//// QuotesFeederProcessor
//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = configuration.GetSection("Trading:Tickers").Get<string[]>() ?? new[] { "AAPL", "MSFT", "GOOGL" };
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//// StrategyProcessor
//builder.Services.AddSingleton<StrategyProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var db = sp.GetRequiredService<InMemoryDatabase>();
//    var lotSize = configuration.GetValue<decimal>("Trading:Strategy:LotSize", 10m);
//    return new StrategyProcessor(bus, loggerFactory, db, lotSize);
//});

//// OrderExecutorProcessor
//builder.Services.AddSingleton<OrderExecutorProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var db = sp.GetRequiredService<InMemoryDatabase>();
//    var fillProbability = configuration.GetValue<double>("Trading:OrderExecutor:FillProbability", 0.7);
//    return new OrderExecutorProcessor(bus, loggerFactory, db, fillProbability);
//});

//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();

//// Принудительно создаём все процессоры, чтобы они подписались на события
//host.Services.GetRequiredService<StrategyProcessor>();
//host.Services.GetRequiredService<OrderExecutorProcessor>();
//host.Services.GetRequiredService<TradeProcessor>();
//host.Services.GetRequiredService<PositionProcessor>();
//host.Services.GetRequiredService<PortfolioProcessor>();

//await host.RunAsync();

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
//builder.Services.AddSingleton<InMemoryDatabase>();

//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();

//// Принудительно создаём все процессоры, чтобы они подписались на события
//host.Services.GetRequiredService<StrategyProcessor>();
//host.Services.GetRequiredService<OrderExecutorProcessor>();
//host.Services.GetRequiredService<TradeProcessor>();
//host.Services.GetRequiredService<PositionProcessor>();
//host.Services.GetRequiredService<PortfolioProcessor>();

//await host.RunAsync();