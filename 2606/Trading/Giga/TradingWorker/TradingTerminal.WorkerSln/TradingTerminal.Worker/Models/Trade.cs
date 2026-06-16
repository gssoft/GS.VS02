// Models/Trade.cs

namespace TradingTerminal.Models;

public class Trade
{
    public string Symbol { get; set; } = "AAPL";
    public decimal Price { get; set; }
    public int Volume { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
