// Program.cs

using System.Threading.Tasks.Dataflow;
using TradingTerminal.Services; // ВАЖНО: Убедитесь, что namespace верный
using TradingTerminal.Worker.Services;

// --- 1. СОЗДАНИЕ ХОСТА ---
var builder = Host.CreateApplicationBuilder(args);

// --- 2. РЕГИСТРАЦИЯ СЕРВИСОВ БИЗНЕС-ЛОГИКИ (Stateful сервисы) ---
// Эти сервисы хранят состояние (ордера, портфель), поэтому регистрируем их как Singleton.
builder.Services.AddSingleton<OrderExecutionService>();
builder.Services.AddSingleton<PortfolioManagementService>();
builder.Services.AddSingleton<UiUpdateService>();


// --- 3. РЕГИСТРАЦИЯ ФОНОВЫХ РАБОТНИКОВ (Background Workers) ---
// Работники управляют жизненным циклом блоков Dataflow.
builder.Services.AddHostedService<MarketDataGeneratorService>();
builder.Services.AddHostedService<OrderExecutionBackgroundWorker>();
builder.Services.AddHostedService<PortfolioManagementBackgroundWorker>();
builder.Services.AddHostedService<UiUpdateBackgroundWorker>();
// 26.06.16
// builder.Services.AddHostedService<UiUpdateBackgroundWorker>();

// --- 4. СОЗДАНИЕ БЛОКОВ DATAFLOW (Конвейер) ---
// Создаем блоки здесь, чтобы передать их в конструкторы Background Worker'ов.

// Источник данных (BufferBlock)
var dataBufferBlock = new BufferBlock<object>(new DataflowBlockOptions { BoundedCapacity = 50 });

// Блоки-обработчики
var uiUpdateBlock = new ActionBlock<object>(async data =>
{
    var service = builder.Services.BuildServiceProvider().GetRequiredService<UiUpdateService>();
    await service.ProcessDataAsync(data);
});
//var uiUpdateBlock = new ActionBlock<object>(async data =>
//{
//    var service = builder.Services.BuildServiceProvider().GetRequiredService<UiUpdateService>();
//    await service.ProcessDataAsync(data);
//});

var orderExecutionBlock = new ActionBlock<object>(async data =>
{
    var service = builder.Services.BuildServiceProvider().GetRequiredService<OrderExecutionService>();
    await service.ProcessDataAsync(data);
});

var portfolioManagerBlock = new ActionBlock<object>(async data =>
{
    var service = builder.Services.BuildServiceProvider().GetRequiredService<PortfolioManagementService>();
    await service.ProcessDataAsync(data);
});

// Broadcaster для распределения данных по обработчикам
var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
var dataBroadcaster = new BroadcastBlock<object>(null);
dataBroadcaster.LinkTo(uiUpdateBlock, linkOptions);
dataBroadcaster.LinkTo(orderExecutionBlock, linkOptions);
dataBroadcaster.LinkTo(portfolioManagerBlock, linkOptions);

// Связываем источник с первым блоком в цепочке
dataBufferBlock.LinkTo(dataBroadcaster, linkOptions);

// --- 5. ПЕРЕДАЧА БЛОКОВ И ТОКЕНА В КОНСТРУКТОРЫ ---
// Передаем созданные блоки и токен отмены в наши фоновые сервисы через лямбда-выражения.
builder.Services.AddSingleton(dataBufferBlock); // Регистрируем источник как синглтон
builder.Services.AddSingleton(uiUpdateBlock);
// 26.06.16
// builder.Services.AddSingleton(uiUpdateBlock);

builder.Services.AddSingleton(orderExecutionBlock);
builder.Services.AddSingleton(portfolioManagerBlock);

// Передача CancellationToken в генератор данных
//builder.Services.AddHostedService(sp =>
//{
//    var logger = sp.GetRequiredService<ILogger<MarketDataGeneratorService>>();
//    return new MarketDataGeneratorService(logger, dataBufferBlock, sp.GetRequiredService<CancellationToken>());
//});

// Стандартная регистрация сервиса. Контейнер сам разберется с зависимостями.
builder.Services.AddHostedService<MarketDataGeneratorService>();

// Остальные работники получают свои блоки через стандартный DI
builder.Services.AddHostedService<OrderExecutionBackgroundWorker>();
builder.Services.AddHostedService<PortfolioManagementBackgroundWorker>();
builder.Services.AddHostedService<UiUpdateBackgroundWorker>();


// --- ЗАПУСК ПРИЛОЖЕНИЯ ---
var host = builder.Build();
host.Run();


