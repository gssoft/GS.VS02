//using System.Net.Http.Json;
//using TradeSystem.Contracts;
//using TradeSystem.Domain;

//namespace TradeSystem.Web.Services;

//public class ApiClientService
//{
//    private readonly HttpClient _http;

//    public ApiClientService(HttpClient http)
//    {
//        _http = http;
//    }

//    public async Task<List<Portfolio>> GetPortfoliosAsync()
//        => await _http.GetFromJsonAsync<List<Portfolio>>("api/portfolios") ?? new();

//    public async Task<Portfolio?> GetPortfolioAsync(Guid id)
//        => await _http.GetFromJsonAsync<Portfolio>($"api/portfolios/{id}");

//    public async Task<List<Ticker>> GetTickersAsync()
//        => await _http.GetFromJsonAsync<List<Ticker>>("api/tickers") ?? new();

//    public async Task<List<TradeStrategy>> GetStrategiesAsync()
//        => await _http.GetFromJsonAsync<List<TradeStrategy>>("api/strategies") ?? new();

//    public async Task<List<TickerStrategyBinding>> GetBindingsAsync(Guid portfolioId)
//        => await _http.GetFromJsonAsync<List<TickerStrategyBinding>>(
//            $"api/bindings?portfolioId={portfolioId}") ?? new();

//    public async Task<PortfolioSnapshot?> GetPortfolioSnapshotAsync(Guid portfolioId)
//        => await _http.GetFromJsonAsync<PortfolioSnapshot>(
//            $"api/portfolios/{portfolioId}/snapshot");

//    public async Task<List<TradeDto>> GetTradesAsync(Guid portfolioId)
//        => await _http.GetFromJsonAsync<List<TradeDto>>(
//            $"api/trades?portfolioId={portfolioId}") ?? new();
//}


using System.Net.Http.Json;
using TradeSystem.Contracts;
using TradeSystem.Domain;

namespace TradeSystem.Web.Services;

public class ApiClientService
{
    private readonly HttpClient _http;

    public ApiClientService(HttpClient http)
    {
        _http = http;
    }

    // --- Портфели ---
    public async Task<List<Portfolio>> GetPortfoliosAsync()
        => await _http.GetFromJsonAsync<List<Portfolio>>("api/portfolios") ?? new();

    public async Task<Portfolio?> GetPortfolioAsync(Guid id)
        => await _http.GetFromJsonAsync<Portfolio>($"api/portfolios/{id}");

    // --- Тикеры ---
    public async Task<List<Ticker>> GetTickersAsync()
        => await _http.GetFromJsonAsync<List<Ticker>>("api/tickers") ?? new();

    // --- Стратегии ---
    public async Task<List<TradeStrategy>> GetStrategiesAsync()
        => await _http.GetFromJsonAsync<List<TradeStrategy>>("api/strategies") ?? new();

    // --- Связки ---
    public async Task<List<TickerStrategyBinding>> GetBindingsAsync(Guid portfolioId)
        => await _http.GetFromJsonAsync<List<TickerStrategyBinding>>(
            $"api/bindings?portfolioId={portfolioId}") ?? new();

    public async Task<StrategyRegisteredEvent?> RegisterStrategyAsync(
        StrategyRegistrationRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/bindings/register", request);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<StrategyRegisteredEvent>();
        return null;
    }

    public async Task<bool> DeactivateBindingAsync(Guid bindingId)
    {
        var response = await _http.PostAsync($"api/bindings/{bindingId}/deactivate", null);
        return response.IsSuccessStatusCode;
    }

    // --- Снимок портфеля ---
    public async Task<PortfolioSnapshot?> GetPortfolioSnapshotAsync(Guid portfolioId)
        => await _http.GetFromJsonAsync<PortfolioSnapshot>(
            $"api/portfolios/{portfolioId}/snapshot");

    // --- История сделок ---
    public async Task<List<TradeDto>> GetTradesAsync(Guid portfolioId)
        => await _http.GetFromJsonAsync<List<TradeDto>>(
            $"api/trades?portfolioId={portfolioId}") ?? new();
}

