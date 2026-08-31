// 26.08.30

using Microsoft.Extensions.Configuration;
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
    private readonly int _loopTimeoutInSec;

    public TradingWorker(
        IMicroEventBus bus,
        QuotesFeederProcessor quotesFeeder,
        ILogger<TradingWorker> logger,
        IConfiguration configuration)
    {
        _bus = bus;
        _quotesFeeder = quotesFeeder;
        _logger = logger;
        _loopTimeoutInSec = configuration.GetValue<int>("Trading:LoopTimeoutInSec", 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trading cycle started with {Interval} sec interval.", _loopTimeoutInSec);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_loopTimeoutInSec));
        try
        {
            int counter = 0;
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                counter++;
                if (counter % 10 == 0)
                    _logger.LogInformation("Generated {Counter} quote cycles", counter);
                await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
            }
            //    try
            //    {
            //        await _quotesFeeder.GenerateQuotesAsync(stoppingToken);
            //    }
            //    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            //    {
            //        break;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Unexpected error in trading cycle.");
            //    }
            //}
        }
        finally
        {
            _logger.LogInformation("Trading cycle stopped gracefully.");
        }
    }
}

//using Microsoft.Extensions.Configuration;
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
//    private readonly int _loopTimeoutInSec;
//    private readonly int _stopTimeoutInSec;

//    public TradingWorker(
//        IMicroEventBus bus,
//        QuotesFeederProcessor quotesFeeder,
//        ILogger<TradingWorker> logger,
//        IConfiguration configuration)
//    {
//        _bus = bus;
//        _quotesFeeder = quotesFeeder;
//        _logger = logger;
//        _loopTimeoutInSec = configuration.GetValue<int>("Trading:LoopTimeoutInSec", 2);
//        _stopTimeoutInSec = configuration.GetValue<int>("Trading:StopTimeoutInSec", 5);
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
//                    await Task.Delay(TimeSpan.FromSeconds(_loopTimeoutInSec), stoppingToken);
//                }
//                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//                {
//                    _logger.LogInformation("Trading cycle stopped by cancellation.");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Unexpected error in trading cycle. Retrying in {StopTimeout} seconds...", _stopTimeoutInSec);
//                    await Task.Delay(TimeSpan.FromSeconds(_stopTimeoutInSec), stoppingToken);
//                }
//            }
//        }
//        finally
//        {
//            _logger.LogInformation("Trading cycle stopped gracefully.");
//        }
//    }
//}


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

//    private readonly int LoopTimeoutInSec = 2;
//    private readonly int StopTimeoutInSec = 5;

//    public TradingWorker(
//        IMicroEventBus bus,
//        QuotesFeederProcessor quotesFeeder,
//        ILogger<TradingWorker> logger)
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