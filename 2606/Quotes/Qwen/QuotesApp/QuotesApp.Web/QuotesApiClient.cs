using System.Net.Http.Json;
using QuotesApp.Shared.Models;

namespace QuotesApp.Web;

public class QuotesApiClient(HttpClient httpClient)
{
    public async Task<List<StockQuote>> GetQuotesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<StockQuote>>("/quotes", ct)
            ?? new List<StockQuote>();
    }

    public async Task<List<StockQuote>> GetQuotesByPortfolioAsync(
        string portfolio, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<StockQuote>>($"/quotes/{portfolio}", ct)
            ?? new List<StockQuote>();
    }
}
