// TradingWorker/Program.cs
using BusMicro;
using System.Threading.Channels;

// Сообщения от ServiceA к ServiceB (Рыночные данные)
public record MarketDataReceived(string Ticker, decimal Price, DateTime Time) : IMessage;

// Сообщения от ServiceB к ServiceA (Торговые данные)
public record OrderPlaced(string OrderId, string Symbol, int Quantity, decimal Price) : IMessage;
public record TradeExecuted(string TradeId, string OrderId, string Symbol, int FilledQuantity, decimal Price) : IMessage;
public record PositionUpdated(string Symbol, int Quantity, decimal AveragePrice) : IMessage;

// TradingWorker/Program.cs

// Обработчики в ServiceB
public class MarketDataLogger : IHandler<MarketDataReceived>
{
    private readonly ILogger<MarketDataLogger> _logger;

    public MarketDataLogger(ILogger<MarketDataLogger> logger) => _logger = logger;

    public Task HandleAsync(MarketDataReceived message, CancellationToken ct)
    {
        _logger.LogInformation($"[SERVICE B ПОЛУЧИЛ] Котировка: {message.Ticker} - {message.Price} в {message.Time}");
        return Task.CompletedTask;
    }
}

// -----------------------------------------
// TradingWorker/Program.cs

// Обработчики в ServiceA
public class TradeEventLogger : IHandler<OrderPlaced>
{
    private readonly ILogger<TradeEventLogger> _logger;

    public TradeEventLogger(ILogger<TradeEventLogger> logger) => _logger = logger;

    public Task HandleAsync(OrderPlaced message, CancellationToken ct)
    {
        _logger.LogInformation($"[SERVICE A ПОЛУЧИЛ] Новый ордер: {message.OrderId} на {message.Quantity} акций {message.Symbol}");
        return Task.CompletedTask;
    }
}

public class TradeExecutionLogger : IHandler<TradeExecuted>
{
    private readonly ILogger<TradeExecutionLogger> _logger;

    public TradeExecutionLogger(ILogger<TradeExecutionLogger> logger) => _logger = logger;

    public Task HandleAsync(TradeExecuted message, CancellationToken ct)
    {
        _logger.LogInformation($"[SERVICE A ПОЛУЧИЛ] Исполнение сделки: {message.TradeId} по ордеру {message.OrderId}");
        return Task.CompletedTask;
    }
}

public class PositionUpdateLogger : IHandler<PositionUpdated>
{
    private readonly ILogger<PositionUpdateLogger> _logger;

    public PositionUpdateLogger(ILogger<PositionUpdateLogger> logger) => _logger = logger;

    public Task HandleAsync(PositionUpdated message, CancellationToken ct)
    {
        _logger.LogInformation($"[SERVICE A ПОЛУЧИЛ] Обновление позиции: {message.Symbol} - {message.Quantity} шт.");
        return Task.CompletedTask;
    }
}
// ---------------------------------------------------------------------

class Program
{

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            // --- НАСТРОЙКА ШИНЫ ---
            var channel = Channel.CreateUnbounded<IMessage>();
            services.AddBusMicro(channel);

            // --- РЕГИСТРАЦИЯ ФОНОВЫХ СЕРВИСОВ ---
            services.AddHostedService<StockMarketService>();
            services.AddHostedService<TradingEngineService>();

            // --- РЕГИСТРАЦИЯ ОБРАБОТЧИКОВ ДЛЯ SERVICEB ---
            // Эти обработчики будут вызываться, когда StockMarketService отправит MarketDataReceived
            services.AddTransient<IHandler<MarketDataReceived>, MarketDataLogger>();

            // --- РЕГИСТРАЦИЯ ОБРАБОТЧИКОВ ДЛЯ SERVICEA ---
            // Эти обработчики будут вызываться, когда TradingEngineService отправит торговые события
            services.AddTransient<IHandler<OrderPlaced>, TradeEventLogger>();
            services.AddTransient<IHandler<TradeExecuted>, TradeExecutionLogger>();
            services.AddTransient<IHandler<PositionUpdated>, PositionUpdateLogger>();
        });
 }


