// QuotesFeederProcessor.cs
using Trading.Core;
using Trading.Domain;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class QuotesFeederProcessor : ProcessorBase<NewQuotes>
{
    private readonly string[] _tickers;
    private readonly Random _rnd = new();

    public QuotesFeederProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, string[] tickers)
        : base(bus, loggerFactory)
    {
        _tickers = tickers;
    }

    public async Task GenerateQuotesAsync(CancellationToken ct)
    {
        var quotes = _tickers.Select(t =>
        {
            decimal basePrice = 100 + (decimal)_rnd.NextDouble() * 50;
            decimal bid = basePrice - 0.1m;
            decimal ask = basePrice + 0.1m;
            return new Quote(t, bid, ask, DateTime.UtcNow);
        }).ToList();

        await Bus.PublishAsync(new NewQuotes(quotes), ct);
    }

    protected override Task HandleAsync(NewQuotes message, CancellationToken ct)
    {
        // QuotesFeeder сам не обрабатывает NewQuotes, он их публикует. 
        // Поэтому можно не переопределять этот метод или использовать отдельный вход.
        return Task.CompletedTask;
    }
}