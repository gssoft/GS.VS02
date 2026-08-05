using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

namespace QuotesApp.Web;

public class QuoteStreamService : IAsyncDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QuoteStreamService> _logger;
    private ISubscriber? _subscriber;

    public event Func<StockQuote, Task>? OnQuoteReceived;

    public QuoteStreamService(IConnectionMultiplexer redis, ILogger<QuoteStreamService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _subscriber = _redis.GetSubscriber();

        await _subscriber.SubscribeAsync(
            RedisChannel.Literal("quotes:all"),
            async (channel, message) =>
            {
                try
                {
                    // Явно преобразуем RedisValue в string
                    var json = message.ToString();

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return;
                    }

                    // Десериализуем уже готовую строку
                    var quote = JsonSerializer.Deserialize<StockQuote>(json);

                    if (quote is not null && OnQuoteReceived is not null)
                    {
                        await OnQuoteReceived.Invoke(quote);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing quote from Redis");
                }
            });

        _logger.LogInformation("Subscribed to quotes:all channel");
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscriber is not null)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal("quotes:all"));
        }
    }
}

//using System.Text.Json;
//using QuotesApp.Shared.Models;
//using StackExchange.Redis;

//namespace QuotesApp.Web;

//public class QuoteStreamService : IAsyncDisposable
//{
//    private readonly IConnectionMultiplexer _redis;
//    private readonly ILogger<QuoteStreamService> _logger;
//    private ISubscriber? _subscriber;

//    public event Func<StockQuote, Task>? OnQuoteReceived;

//    public QuoteStreamService(IConnectionMultiplexer redis, ILogger<QuoteStreamService> logger)
//    {
//        _redis = redis;
//        _logger = logger;
//    }

//    public async Task StartAsync()
//    {
//        _subscriber = _redis.GetSubscriber();

//        await _subscriber.SubscribeAsync(
//            RedisChannel.Literal("quotes:all"),
//            async (channel, message) =>
//            {
//                try
//                {
//                    var quote = JsonSerializer.Deserialize<StockQuote>(message!);
//                    if (quote is not null && OnQuoteReceived is not null)
//                    {
//                        await OnQuoteReceived.Invoke(quote);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error deserializing quote from Redis");
//                }
//            });

//        _logger.LogInformation("Subscribed to quotes:all channel");
//    }

//    public async ValueTask DisposeAsync()
//    {
//        if (_subscriber is not null)
//        {
//            await _subscriber.UnsubscribeAsync(RedisChannel.Literal("quotes:all"));
//        }
//    }
//}
