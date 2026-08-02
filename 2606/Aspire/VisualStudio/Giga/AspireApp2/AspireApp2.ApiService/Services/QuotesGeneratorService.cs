namespace AspireApp.ApiService.Service;
// File: Services/QuotesGeneratorService.cs
using Quotes.Shared.Contracts;
using System.Threading.Channels;

public class QuotesGeneratorService : BackgroundService
{
    private readonly ILogger<QuotesGeneratorService> _logger;
    private readonly ChannelWriter<QuoteDto> _writer;
    private static readonly string[] FinanceTickers = { "SBER", "GAZP", "LKOH", "YNDX", "TATN" };
    private static readonly string[] Summaries = { "Up", "Down", "Flat", "Volatile" };
    private readonly Random _rand = new();

    public QuotesGeneratorService(ILogger<QuotesGeneratorService> logger, ChannelWriter<QuoteDto> writer)
    {
        _logger = logger;
        _writer = writer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var ticker = FinanceTickers[_rand.Next(FinanceTickers.Length)];
            var basePrice = _rand.Next(150, 450) + _rand.NextDecimal(); // Цена от 150 до 450 руб.

            var quote = new QuoteDto(
                Ticker: ticker,
                Last: Math.Round(basePrice, 2),
                Volume: _rand.Next(10, 5000),
                TimestampUtc: DateTimeOffset.UtcNow);

            // Логируем каждый тик (ваша задача №1)
            _logger.LogInformation("Generated tick: {@Quote}", quote);

            await _writer.WriteAsync(quote, stoppingToken);

            // Частота обновления котировок
            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }
}

// Extension for Random to get decimals
public static class RandomExtensions
{
    public static decimal NextDecimal(this Random random)
    {
        return (decimal)random.NextDouble();
    }
}
