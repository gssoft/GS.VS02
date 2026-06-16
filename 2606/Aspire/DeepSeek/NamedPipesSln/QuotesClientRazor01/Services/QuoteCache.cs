// Services/QuoteCache.cs

using System.Collections.Concurrent;
using QuotesClientRazor01.Models;

namespace QuotesClientRazor01.Services;

public class QuoteCache
{
    private readonly ConcurrentDictionary<string, StockQuote> _latestQuotes = new();
    private readonly ConcurrentDictionary<string, decimal> _previousPrices = new();
    private readonly ILogger<QuoteCache> _logger;

    public IReadOnlyDictionary<string, StockQuote> LatestQuotes => _latestQuotes;

    public event EventHandler<StockQuote>? QuoteUpdated;

    public QuoteCache(ILogger<QuoteCache> logger)
    {
        _logger = logger;
    }

    public void UpdateQuote(StockQuote quote)
    {
        // Сохраняем предыдущую цену для расчета изменения
        if (_latestQuotes.TryGetValue(quote.Ticker, out var previous))
        {
            _previousPrices[quote.Ticker] = previous.Last;
        }
        else
        {
            _previousPrices[quote.Ticker] = quote.Last;
        }

        // Рассчитываем изменение
        if (_previousPrices.TryGetValue(quote.Ticker, out var previousPrice))
        {
            quote.Change = quote.Last - previousPrice;
            quote.ChangePercent = previousPrice != 0
                ? (quote.Change / previousPrice) * 100
                : 0;
        }

        _latestQuotes[quote.Ticker] = quote;

        _logger.LogDebug($"Cache updated: {quote.Ticker} @ {quote.Last:C}");

        // Уведомляем подписчиков
        QuoteUpdated?.Invoke(this, quote);
    }

    public StockQuote? GetQuote(string ticker)
    {
        return _latestQuotes.TryGetValue(ticker, out var quote) ? quote : null;
    }

    public void InitializeTickers(List<string> tickers)
    {
        foreach (var ticker in tickers)
        {
            _latestQuotes[ticker] = new StockQuote
            {
                Ticker = ticker,
                Timestamp = DateTime.Now,
                Last = 0,
                Bid = 0,
                Ask = 0,
                Volume = 0,
                Change = 0,
                ChangePercent = 0
            };
        }
    }
}
