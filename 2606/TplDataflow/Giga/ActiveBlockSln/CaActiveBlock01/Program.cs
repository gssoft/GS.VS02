// Program.cs

using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TradingTerminal.Models;
using TradingTerminal.Providers;
using TradingTerminal.Services;

// Создаем экземпляры наших сервисов бизнес-логики
var orderService = new OrderExecutionService();
var portfolioService = new PortfolioManagementService();
var uiService = new PortfolioManagementService(); // Можно использовать тот же сервис или создать отдельный для UI

// --- СОЗДАЕМ БЛОКИ DATAFLOW ---
// 1. Блок для логирования в консоль (наш "UI")
var uiUpdateBlock = new ActionBlock<object>(data =>
{
    string message = data switch
    {
        Quote q => $"[UI] Котировка: {q.Symbol} @ {q.Price:C2}",
        Trade t => $"[UI] Сделка: {t.Volume} лотов @ {t.Price:C2}",
        _ => "Неизвестное событие"
    };
    Console.WriteLine(message);
});

// 2. Блок для исполнения ордеров, использующий логику из OrderExecutionService
var orderExecutionBlock = new ActionBlock<object>(data => orderService.ProcessDataAsync(data));

// 3. Блок для управления портфелем, использующий логику из PortfolioManagementService
var portfolioManagerBlock = new ActionBlock<object>(data => portfolioService.ProcessDataAsync(data));


// --- СБОРКА КОНВЕЙЕРА ---
var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

// Создаем BroadcastBlock для распределения данных по нескольким потребителям.
// Он будет нашим "входом" в систему.
var dataBroadcaster = new BroadcastBlock<object>(null);
dataBroadcaster.LinkTo(uiUpdateBlock, linkOptions);
dataBroadcaster.LinkTo(orderExecutionBlock, linkOptions);
dataBroadcaster.LinkTo(portfolioManagerBlock, linkOptions);


// --- ЗАПУСК ПРОЦЕССА ---
_ = Task.Run(async () =>
{
    await foreach (var marketData in MarketDataProvider.GetMarketDataAsync())
    {
        // Отправляем данные в начало конвейера.
        if (!dataBroadcaster.Post(marketData))
        {
            // Если очередь переполнена, подождем и попробуем снова.
            await Task.Delay(50);
            dataBroadcaster.Post(marketData);
        }
    }
});


// --- ОЖИДАНИЕ ЗАВЕРШЕНИЯ ---
Console.WriteLine("Торговый терминал запущен. Нажмите Enter для выхода...");
Console.ReadLine();
Console.WriteLine("Инициируем завершение работы...");


// Завершаем работу источника данных.
dataBroadcaster.Complete();

// Ждем завершения всех блоков в конвейере.
await Task.WhenAll(
    uiUpdateBlock.Completion,
    orderExecutionBlock.Completion,
    portfolioManagerBlock.Completion
);

Console.WriteLine("Система остановлена.");

