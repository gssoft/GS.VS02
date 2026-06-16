// Services/UiUpdateBackgroundWorker.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Threading;
using TradingTerminal.Services; // Для доступа к OrderExecutionService

public class UiUpdateBackgroundWorker : BackgroundService
{
    private readonly ILogger<UiUpdateBackgroundWorker> _logger;
    private readonly UiUpdateService _uiUpdateService; // <-- ИСПРАВЛЕНО
    private readonly ActionBlock<object> _uiUpdateBlock; // <-- ИСПРАВЛЕНО

    public UiUpdateBackgroundWorker(
        ILogger<UiUpdateBackgroundWorker> logger,
        UiUpdateService uiUpdateService, // <-- ИСПРАВЛЕНО
        ActionBlock<object> uiUpdateBlock) // <-- ИСПРАВЛЕНО
    {
        _logger = logger;
        _uiUpdateService = uiUpdateService; // <-- ИСПРАВЛЕНО
        _uiUpdateBlock = uiUpdateBlock; // <-- ИСПРАВЛЕНО
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис обновления UI запущен."); // <-- ИСПРАВЛЕНО сообщение

        // Ждем завершения блока-обработчика.
        await _uiUpdateBlock.Completion; // <-- ИСПРАВЛЕНО

        _logger.LogInformation("Сервис обновления UI остановлен."); // <-- ИСПРАВЛЕНО сообщение
    }
}

//public class UiUpdateBackgroundWorker : BackgroundService
//{
//    private readonly ILogger<UiUpdateBackgroundWorker> _logger;
//    private readonly OrderExecutionService _orderExecutionService;
//    private readonly ActionBlock<object> _orderExecutionBlock; // Блок-обработчик

//    public UiUpdateBackgroundWorker(
//        ILogger<UiUpdateBackgroundWorker> logger,
//        OrderExecutionService orderExecutionService,
//        ActionBlock<object> orderExecutionBlock)
//    {
//        _logger = logger;
//        _orderExecutionService = orderExecutionService;
//        _orderExecutionBlock = orderExecutionBlock;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Сервис исполнения ордеров запущен.");

//        // Ждем завершения блока-обработчика.
//        // Это произойдет, когда источник данных вызовет Complete() и все элементы будут обработаны.
//        await _orderExecutionBlock.Completion;

//        _logger.LogInformation("Сервис исполнения ордеров остановлен.");
//    }
//}
