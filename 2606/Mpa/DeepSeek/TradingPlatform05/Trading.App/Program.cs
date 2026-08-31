//// 26.08.30

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Trading.App;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Создаём логгер вручную и регистрируем
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);

// Загружаем манифест
var manifestPath = builder.Configuration["ManifestPath"] ?? "manifest.json";
var manifest = ManifestLoader.Load(manifestPath);

// Создаём общие компоненты
var bus = new InMemoryMicroEventBus();
var db = new InMemoryDatabase();

// Регистрируем шину и БД
builder.Services.AddSingleton<IMicroEventBus>(bus);
builder.Services.AddSingleton<InMemoryDatabase>(db);

// Создаём процессоры из манифеста
var processors = ManifestLoader.CreateProcessors(manifest, bus, loggerFactory, db);

// Регистрируем процессоры в DI
foreach (var processor in processors)
{
    builder.Services.AddSingleton(processor.GetType(), processor);
}

// Регистрируем TradingWorker как hosted service
builder.Services.AddHostedService<TradingWorker>();

var app = builder.Build();

// Принудительно создаём все процессоры, чтобы они подписались на события
foreach (var processor in processors)
{
    // Уже созданы, но можно вызвать GetRequiredService для надёжности
    app.Services.GetRequiredService(processor.GetType());
}

// --- Minimal API для отображения ---

//app.MapGet("/", async context =>
//{
//    context.Response.ContentType = "text/html; charset=utf-8";
//    await context.Response.WriteAsync(@"
//<!DOCTYPE html>
//<html>
//<head><title>Trading Monitor</title></head>
//<body>
//<h1>Trading Monitor</h1>
//<div id='events'></div>
//<script>
//async function fetchData() {
//    const resp = await fetch('/events');
//    const data = await resp.json();
//    const div = document.getElementById('events');
//    div.innerHTML = '<pre>' + JSON.stringify(data, null, 2) + '</pre>';
//}
//setInterval(fetchData, 1000);
//fetchData();
//</script>
//</body>
//</html>");
//});

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Trading Monitor</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        h1 { color: #333; }
        table { border-collapse: collapse; width: 100%; margin-top: 10px; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        tr:nth-child(even) { background-color: #f9f9f9; }
        .event-type { font-weight: bold; }
    </style>
</head>
<body>
    <h1>Trading Monitor</h1>
    <div id='last-updated'></div>
    <table id='events-table'>
        <thead>
            <tr>
                <th>Event Type</th>
                <th>Ticker</th>
                <th>Quantity</th>
                <th>Price</th>
                <th>Side</th>
                <th>Timestamp</th>
                <th>Details</th>
            </tr>
        </thead>
        <tbody>
        </tbody>
    </table>

    <script>
        async function fetchData() {
            try {
                const resp = await fetch('/events');
                const events = await resp.json();
                const tbody = document.querySelector('#events-table tbody');
                tbody.innerHTML = '';
                events.forEach(evt => {
                    const row = document.createElement('tr');
                    const eventType = evt.constructor?.name || evt.$type || (evt.tradeId ? 'Trade' : evt.orderId && evt.side ? 'Order' : evt.position ? 'Position' : 'Event');
                    let ticker = '', quantity = '', price = '', side = '', timestamp = '', details = '';
                    if (evt.tradeId) {
                        ticker = evt.ticker || '';
                        quantity = evt.quantity || '';
                        price = evt.price || '';
                        side = evt.side || '';
                        timestamp = evt.timestamp || '';
                        details = 'TradeId: ' + evt.tradeId + ', OrderId: ' + evt.orderId;
                    } else if (evt.orderId && evt.side) {
                        ticker = evt.ticker || '';
                        quantity = evt.quantity || '';
                        price = evt.price || '';
                        side = evt.side || '';
                        timestamp = evt.timestamp || '';
                        details = 'OrderId: ' + evt.orderId;
                    } else if (evt.ticker && evt.quantity !== undefined) {
                        ticker = evt.ticker || '';
                        quantity = evt.quantity || '';
                        price = evt.averagePrice || '';
                        timestamp = new Date().toISOString(); // Position events don't have timestamp
                        details = 'Position updated';
                    } else if (evt.positions) {
                        ticker = '';
                        quantity = '';
                        price = '';
                        timestamp = new Date().toISOString();
                        details = 'Portfolio snapshot: ' + JSON.stringify(evt.positions);
                    } else {
                        details = JSON.stringify(evt);
                    }
                    row.innerHTML = `
                        <td class='event-type'>${eventType}</td>
                        <td>${ticker}</td>
                        <td>${quantity}</td>
                        <td>${price}</td>
                        <td>${side}</td>
                        <td>${timestamp}</td>
                        <td>${details}</td>
                    `;
                    tbody.appendChild(row);
                });
                document.getElementById('last-updated').textContent = 'Last updated: ' + new Date().toLocaleTimeString();
            } catch (err) {
                console.error('Error fetching events:', err);
            }
        }
        setInterval(fetchData, 1000);
        fetchData();
    </script>
</body>
</html>");
});

app.MapGet("/events", (InMemoryDatabase db) => db.EventLog.ToArray());
app.MapGet("/positions", (InMemoryDatabase db) => db.Positions.Values);
app.MapGet("/trades", (InMemoryDatabase db) => db.Trades.Values);
app.MapGet("/orders", (InMemoryDatabase db) => db.Orders.Values);

// Запуск приложения (это запустит и hosted services)
await app.RunAsync();


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
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//// Создаём логгер вручную (или можно через DI, но проще для загрузчика)
//using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
//builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);

//// Загружаем манифест
//var manifestPath = builder.Configuration["ManifestPath"] ?? "manifest.json";
//var manifest = ManifestLoader.Load(manifestPath);

//// Создаём общие компоненты
//var bus = new InMemoryMicroEventBus();
//var db = new InMemoryDatabase();

//// Создаём процессоры из манифеста
//var processors = ManifestLoader.CreateProcessors(manifest, bus, loggerFactory, db);

//// Регистрируем в DI
//builder.Services.AddSingleton<IMicroEventBus>(bus);
//builder.Services.AddSingleton<InMemoryDatabase>(db);

//foreach (var processor in processors)
//{
//    builder.Services.AddSingleton(processor.GetType(), processor);
//}

//// Регистрируем TradingWorker
//builder.Services.AddHostedService<TradingWorker>();

//var host = builder.Build();
//await host.RunAsync();


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