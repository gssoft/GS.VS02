using Trading.App;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Шина
builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();

// Хранилище
builder.Services.AddSingleton<InMemoryDatabase>();

// Процессоры (singleton, чтобы жили всё время)
builder.Services.AddSingleton<QuotesFeederProcessor>();
builder.Services.AddSingleton<StrategyProcessor>();
builder.Services.AddSingleton<OrderExecutorProcessor>();
builder.Services.AddSingleton<TradeProcessor>();
builder.Services.AddSingleton<PositionProcessor>();
builder.Services.AddSingleton<PortfolioProcessor>();

// Worker
builder.Services.AddHostedService<TradingWorker>();

var host = builder.Build();
await host.RunAsync();
