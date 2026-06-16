// Models/Quote.cs

namespace TradingTerminal.Models;

public class Quote
{
    public string Symbol { get; set; } = "AAPL";
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
