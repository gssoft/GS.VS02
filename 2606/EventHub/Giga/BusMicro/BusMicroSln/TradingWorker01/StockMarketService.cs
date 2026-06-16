// TradingWorker/Program.cs

// ФОНДОВЫЙ СЕРВИС (ServiceA) - Генерирует котировки
using BusMicro;

public class StockMarketService : BackgroundService
{
    private readonly IMessageBus _bus;
    private readonly ILogger<StockMarketService> _logger;
    private readonly string[] _tickers = { "GAZP", "SBER", "LKOH", "ROSN" };
    private readonly Random _random = new Random();

    public StockMarketService(IMessageBus bus, ILogger<StockMarketService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SERVICE A] Запуск генерации котировок...");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Генерируем случайную котировку
                var ticker = _tickers[_random.Next(_tickers.Length)];
                var price = 100m + (decimal)_random.NextDouble() * 200; // Цена от 100 до 300

                var marketData = new MarketDataReceived(ticker, price, DateTime.UtcNow);
                await _bus.PublishAsync(marketData, stoppingToken);
                _logger.LogDebug($"[SERVICE A ОТПРАВИЛ] Котировка: {ticker} - {price}");

                await Task.Delay(1000, stoppingToken); // Раз в секунду
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OperationCanceledException");
            }
            catch (Exception)
            {
                _logger.LogWarning("Error occurred while publishing event Exception");
                // throw;
            }
            finally
            {
                _logger.LogWarning("That's right");
            }

        }
    }
}

// ТОРГОВЫЙ СЕРВИС (ServiceB) - Генерирует торговые события
public class TradingEngineService : BackgroundService
{
    private readonly IMessageBus _bus;
    private readonly ILogger<TradingEngineService> _logger;
    private readonly Random _random = new Random();

    public TradingEngineService(IMessageBus bus, ILogger<TradingEngineService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SERVICE B] Запуск торгового движка...");
        var orderCounter = 1;
        while (!stoppingToken.IsCancellationRequested)
        {
            // Случайным образом генерируем разные типы торговых событий
            var eventType = _random.Next(3);
            string orderId = $"ORD-{orderCounter++}";

            try
            {
                switch (eventType)
                {
                    case 0:
                        var order = new OrderPlaced(orderId, "SBER", _random.Next(1, 101), 250.50m);
                        await _bus.PublishAsync(order, stoppingToken);
                        break;
                    case 1:
                        var trade = new TradeExecuted($"TRD-{orderCounter++}", orderId, "SBER", _random.Next(1, 101), 250.75m);
                        await _bus.PublishAsync(trade, stoppingToken);
                        break;
                    case 2:
                        var position = new PositionUpdated("SBER", _random.Next(-50, 51), 245.00m);
                        await _bus.PublishAsync(position, stoppingToken);
                        break;
                }
                await Task.Delay(1000, stoppingToken); // Раз в секунду
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OperationCanceledException");
            }
            catch (Exception)
            {
                _logger.LogWarning("Error occurred while publishing event Exception");
                // throw;
            }
            finally
            {
                _logger.LogWarning("That's right");
            }
        }
    }
}

