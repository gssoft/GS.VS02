public class TradingApiClient
{
    private readonly HttpClient _http;

    public TradingApiClient(HttpClient http) => _http = http;

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _http.GetFromJsonAsync<List<Order>>("/api/orders") ?? new();
    }

    public async Task<Order?> PlaceOrderAsync(Order order)
    {
        var response = await _http.PostAsJsonAsync("/api/orders", order);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<MarketData?> GetMarketDataAsync(string symbol)
    {
        return await _http.GetFromJsonAsync<MarketData>($"/api/marketdata/{symbol}");
    }
}

public record Order
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Side { get; set; } = "Buy";
    public DateTime CreatedAt { get; set; }
}

public record MarketData(string Symbol, decimal Price, DateTime Timestamp);
