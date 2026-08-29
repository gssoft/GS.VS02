using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;

class Program
{
    static async Task Main(string[] args)
    {
        // Настраиваем логгер для вывода в консоль
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Создаём шину и базу данных
        var bus = new InMemoryMicroEventBus();
        var db = new InMemoryDatabase();
        var tickers = new[] { "AAPL", "MSFT", "GOOGL" };

        // Создаём все процессоры (они подпишутся на события в конструкторах)
        var quotesFeeder = new QuotesFeederProcessor(bus, loggerFactory, tickers);
        var strategy = new StrategyProcessor(bus, loggerFactory, db);
        var executor = new OrderExecutorProcessor(bus, loggerFactory, db);
        var tradeProcessor = new TradeProcessor(bus, loggerFactory, db);
        var positionProcessor = new PositionProcessor(bus, loggerFactory, db);
        var portfolioProcessor = new PortfolioProcessor(bus, loggerFactory, db);

        Console.WriteLine("All processors created.");

        // Запускаем цикл генерации котировок в фоновом режиме
        using var cts = new CancellationTokenSource();
        var workerTask = Task.Run(async () =>
        {
            var logger = loggerFactory.CreateLogger("TradingWorker");
            logger.LogInformation("Trading cycle started.");

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await quotesFeeder.GenerateQuotesAsync(cts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Trading cycle stopped by cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in trading cycle.");
                    await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                }
            }

            logger.LogInformation("Trading cycle stopped gracefully.");
        });

        Console.WriteLine("Trading started. Press Ctrl+C to exit.");

        // Обработка Ctrl+C для корректной остановки
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // не завершаем процесс сразу
            cts.Cancel();     // даём сигнал остановки
        };

        try
        {
            await workerTask;
        }
        catch (OperationCanceledException)
        {
            // нормальное завершение
        }

        // Даём процессорам завершить обработку оставшихся сообщений
        await Task.Delay(500);

        Console.WriteLine("Application stopped.");
    }
}

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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
//host.Services.GetRequiredService<QuotesFeederProcessor>();

//Console.WriteLine("Processors created, starting host...");

//await host.RunAsync();

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//Console.WriteLine("Host built, starting...");

//// === НАСТРОЙКА ЛОГИРОВАНИЯ (ДОБАВЛЕНО) ===
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Information);
//// =========================================

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

//Console.WriteLine("Host starting");

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

////builder.Logging.ClearProviders();
////builder.Logging.AddConsole();
////builder.Logging.SetMinimumLevel(LogLevel.Debug);

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
//await host.RunAsync();

//// Program.cs
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Debug);

//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
//builder.Services.AddSingleton<InMemoryDatabase>();

//// QuotesFeederProcessor с явным указанием тикеров
//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//// Остальные процессоры как singleton (без AddHostedService)
//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//// Только TradingWorker как hosted service
//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();

// 26.08.29
//// Program.cs
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//// Console.OutputEncoding = System.Text.Encoding.UTF8;

//var builder = Host.CreateApplicationBuilder(args);

//// Явно добавляем консольный логгер и устанавливаем минимальный уровень
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Debug); // или Information

//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
//builder.Services.AddSingleton<InMemoryDatabase>();

//// QuotesFeederProcessor с явным указанием тикеров
//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//// Остальные процессоры
//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//// Регистрируем все процессоры как Hosted Services
//builder.Services.AddHostedService(sp => sp.GetRequiredService<QuotesFeederProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<StrategyProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderExecutorProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<TradeProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<PositionProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<PortfolioProcessor>());

//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();


//// 26.08.29
//// Program.cs

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();
//builder.Services.AddSingleton<InMemoryDatabase>();

//// QuotesFeederProcessor с явным указанием тикеров
//builder.Services.AddSingleton<QuotesFeederProcessor>(sp =>
//{
//    var bus = sp.GetRequiredService<IMicroEventBus>();
//    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
//    var tickers = new[] { "AAPL", "MSFT", "GOOGL" };
//    return new QuotesFeederProcessor(bus, loggerFactory, tickers);
//});

//// Остальные процессоры
//builder.Services.AddSingleton<StrategyProcessor>();
//builder.Services.AddSingleton<OrderExecutorProcessor>();
//builder.Services.AddSingleton<TradeProcessor>();
//builder.Services.AddSingleton<PositionProcessor>();
//builder.Services.AddSingleton<PortfolioProcessor>();

//// Регистрируем все процессоры как Hosted Services
//builder.Services.AddHostedService(sp => sp.GetRequiredService<QuotesFeederProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<StrategyProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderExecutorProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<TradeProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<PositionProcessor>());
//builder.Services.AddHostedService(sp => sp.GetRequiredService<PortfolioProcessor>());

//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();

// 26.08.26
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.App;
//using Trading.Core;
//using Trading.Processors;
//using Trading.Storage;

//var builder = Host.CreateApplicationBuilder(args);

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

