// 26.08.30

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