// Models/LimitOrder.cs

namespace TradingTerminal.Models;

public class LimitOrder
{
    public string Symbol { get; set; } = "AAPL";
    public decimal LimitPrice { get; set; }
    public int Volume { get; set; }
}
