////// 26.08.31
///

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.App;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;
using Trading.EventBus.RabbitMQ;

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

// Выбираем шину событий
IMicroEventBus bus = builder.Configuration["EventBus:Type"] switch
{
    "RabbitMQ" => new RabbitMqMicroEventBus(
        builder.Configuration["EventBus:RabbitMQ:HostName"] ?? "127.0.0.1",
        builder.Configuration.GetValue<int>("EventBus:RabbitMQ:Port", 15672),
        builder.Configuration["EventBus:RabbitMQ:Username"] ?? "guest",
        builder.Configuration["EventBus:RabbitMQ:Password"] ?? "guest"),
    _ => new InMemoryMicroEventBus()
};

// Создаём хранилище
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
    app.Services.GetRequiredService(processor.GetType());
}

// --- Minimal API для отображения ---

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
                    events.slice(-42).reverse().forEach(entry => {
              /*  events.forEach(entry => { */
                    const evt = entry.data;
                    const eventType = entry.eventType;
                    let ticker = evt.ticker || '';
                    let quantity = evt.quantity !== undefined ? evt.quantity : '';
                    let price = evt.price !== undefined ? evt.price : '';
                    let side = evt.side || '';
                    let timestamp = evt.timestamp || new Date().toISOString();
                    let details = '';

                    switch(eventType) {
                        case 'OrderCreated':
                        case 'OrderFilled':
                            details = 'OrderId: ' + (evt.orderId || '');
                            break;
                        case 'OrderNotFilled':
                            details = 'Reason: ' + (evt.reason || '');
                            break;
                        case 'Trade':
                            details = 'TradeId: ' + (evt.tradeId || '') + ', OrderId: ' + (evt.orderId || '');
                            break;
                        case 'PositionUpdated':
                            details = 'AveragePrice: ' + (evt.averagePrice || '');
                            break;
                        case 'PortfolioSnapshot':
                            ticker = '';
                            quantity = '';
                            price = '';
                            details = JSON.stringify(evt.positions);
                            break;
                        default:
                            details = JSON.stringify(evt);
                    }

                    const row = document.createElement('tr');
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

// Запуск приложения
await app.RunAsync();
