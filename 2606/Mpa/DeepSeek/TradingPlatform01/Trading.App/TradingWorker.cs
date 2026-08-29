using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.Core;
using Trading.Processors;

namespace Trading.App;

public class TradingWorker : BackgroundService
{
    private readonly IMicroEventBus _bus;
    private readonly QuotesFeederProcessor _quotesFeeder;
    private readonly ILogger<TradingWorker> _logger;
    private readonly StrategyProcessor _strategyProcessor;
    private readonly OrderExecutorProcessor _orderExecutorProcessor;
    private readonly TradeProcessor _tradeProcessor;
    private readonly PositionProcessor _positionProcessor;
    private readonly PortfolioProcessor _portfolioProcessor;

    private readonly int LoopTimeoutInSec = 2;
    private readonly int StopTimeoutInSec = 5;

    public TradingWorker(
        IMicroEventBus bus,
        QuotesFeederProcessor quotesFeeder,
        ILogger<TradingWorker> logger,
        StrategyProcessor strategyProcessor,
        OrderExecutorProcessor orderExecutorProcessor,
        TradeProcessor tradeProcessor,
        PositionProcessor positionProcessor,
        PortfolioProcessor portfolioProcessor)
    {
        _bus = bus;
        _quotesFeeder = quotesFeeder;
        _logger = logger;
        _strategyProcessor = strategyProcessor;
        _orderExecutorProcessor = orderExecutorProcessor;
        _tradeProcessor = tradeProcessor;
        _positionProcessor = positionProcessor;
        _portfolioProcessor = portfolioProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("TradingWorker ExecuteAsync started.");
        Console.WriteLine("Console: TradingWorker started");
        Console.WriteLine("Before logger info");
        // Все процессоры уже созданы и подписаны на события благодаря DI
        _logger.LogInformation("Trading cycle started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(LoopTimeoutInSec), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Trading cycle stopped by cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Unexpected error in trading cycle. Retrying in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(StopTimeoutInSec), stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Trading cycle stopped gracefully.");
        }
    }
}

//// TradingWorker.cs
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Trading.Core;
//using Trading.Processors;

//namespace Trading.App;

//public class TradingWorker : BackgroundService
//{
//    private readonly IMicroEventBus _bus;
//    private readonly QuotesFeederProcessor _quotesFeeder;
//    private readonly ILogger<TradingWorker> _logger;
//    private readonly IServiceProvider _serviceProvider;

//    private readonly int LoopTimeoutInSec = 2;
//    private readonly int StopTimeoutInSec = 5;

//    public TradingWorker(
//        IMicroEventBus bus,
//        QuotesFeederProcessor quotesFeeder,
//        ILogger<TradingWorker> logger,
//        IServiceProvider serviceProvider)
//    {
//        _bus = bus;
//        _quotesFeeder = quotesFeeder;
//        _logger = logger;
//        _serviceProvider = serviceProvider;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        // Явно создаём все процессоры, чтобы они подписались на события
//        _serviceProvider.GetRequiredService<StrategyProcessor>();
//        _serviceProvider.GetRequiredService<OrderExecutorProcessor>();
//        _serviceProvider.GetRequiredService<TradeProcessor>();
//        _serviceProvider.GetRequiredService<PositionProcessor>();
//        _serviceProvider.GetRequiredService<PortfolioProcessor>();

//        _logger.LogInformation("Trading cycle started.");

//        try
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
//                    await Task.Delay(TimeSpan.FromSeconds(LoopTimeoutInSec), stoppingToken);
//                }
//                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//                {
//                    _logger.LogInformation("Trading cycle stopped by cancellation.");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Unexpected error in trading cycle. Retrying in 5 seconds...");
//                    await Task.Delay(TimeSpan.FromSeconds(StopTimeoutInSec), stoppingToken);
//                }
//            }
//        }
//        finally
//        {
//            _logger.LogInformation("Trading cycle stopped gracefully.");
//        }
//    }
//}

// 26.08.29
// TradingWorker.cs

//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System.Collections.ObjectModel;
//using Trading.Core;
//using Trading.Processors;

//namespace Trading.App;

//public class TradingWorker : BackgroundService
//{
//    private readonly IMicroEventBus _bus;
//    private readonly QuotesFeederProcessor _quotesFeeder;
//    private readonly ILogger<TradingWorker> _logger;

//    private readonly int LoopTimeoutInSec = 2;
//    private readonly int StopTimeoutInSec = 5;


//    public TradingWorker(
//        IMicroEventBus bus,
//        QuotesFeederProcessor quotesFeeder,
//        ILogger<TradingWorker> logger
//        )
//    {
//        _bus = bus;
//        _quotesFeeder = quotesFeeder;
//        _logger = logger;    
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Trading cycle started.");

//        try
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
//                    await Task.Delay(TimeSpan.FromSeconds(LoopTimeoutInSec), stoppingToken);
//                }
//                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//                {
//                    // Это штатная остановка, выходим из цикла
//                    _logger.LogError("Это штатная остановка, выходим из цикла");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    // Непредвиденная ошибка: логируем и делаем паузу перед повтором
//                    _logger.LogError(ex, "Unexpected error in trading cycle. Retrying in 5 seconds...");
//                    await Task.Delay(TimeSpan.FromSeconds(StopTimeoutInSec), stoppingToken);
//                }
//            }
//        }
//        finally
//        {
//            _logger.LogInformation("Trading cycle stopped gracefully.");
//        }
//    }
//}
