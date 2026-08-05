using QuotesApp.Shared.Models;

namespace QuotesApp.ProviderCore.Abstractions;

public interface IQuoteFetcher
{
    Task<StockQuote> FetchAsync(string ticker, string portfolio, CancellationToken ct);
}
