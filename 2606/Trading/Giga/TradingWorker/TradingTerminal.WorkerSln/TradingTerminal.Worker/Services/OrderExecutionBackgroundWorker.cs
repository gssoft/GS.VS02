// Services/OrderExecutionBackgroundWorker.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Threading;
using TradingTerminal.Services; // Для доступа к OrderExecutionService

public class OrderExecutionBackgroundWorker : BackgroundService
{
    private readonly ILogger<OrderExecutionBackgroundWorker> _logger;
    private readonly OrderExecutionService _orderExecutionService;
    private readonly ActionBlock<object> _orderExecutionBlock; // Блок-обработчик

    public OrderExecutionBackgroundWorker(
        ILogger<OrderExecutionBackgroundWorker> logger,
        OrderExecutionService orderExecutionService,
        ActionBlock<object> orderExecutionBlock)
    {
        _logger = logger;
        _orderExecutionService = orderExecutionService;
        _orderExecutionBlock = orderExecutionBlock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис исполнения ордеров запущен.");

        // Ждем завершения блока-обработчика.
        // Это произойдет, когда источник данных вызовет Complete() и все элементы будут обработаны.
        await _orderExecutionBlock.Completion;

        _logger.LogInformation("Сервис исполнения ордеров остановлен.");
    }
}