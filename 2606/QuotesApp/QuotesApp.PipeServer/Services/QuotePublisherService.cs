using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

namespace QuotesApp.PipeServer.Services;

public class QuotePublisherService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QuotePublisherService> _logger;
    private readonly int _intervalMs;

    public QuotePublisherService(
        IConnectionMultiplexer redis,
        ILogger<QuotePublisherService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _intervalMs = configuration.GetValue<int>("QuoteSettings:IntervalMs", 1000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuotePublisherService started. Interval: {Interval}ms", _intervalMs);

        var subscriber = _redis.GetSubscriber();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var (portfolioKey, config) in PortfolioDefinition.Portfolios)
                {
                    foreach (var ticker in config.Tickers)
                    {
                        var quote = QuoteGenerator.Generate(ticker, portfolioKey);
                        var json = JsonSerializer.Serialize(quote);

                        await subscriber.PublishAsync(
                            RedisChannel.Literal($"quotes:{portfolioKey}"), json);

                        await subscriber.PublishAsync(
                            RedisChannel.Literal("quotes:all"), json);

                        await _redis.GetDatabase().HashSetAsync(
                            $"dashboard:{portfolioKey}", ticker, json);
                    }
                }

                _logger.LogDebug("Published quotes for all portfolios");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error publishing quotes");
            }

            await Task.Delay(_intervalMs, stoppingToken);
        }
    }
}
