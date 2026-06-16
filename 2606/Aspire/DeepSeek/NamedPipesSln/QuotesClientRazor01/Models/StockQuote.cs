// Models/StockQuotes.cs

namespace QuotesClientRazor01.Models;

public class StockQuote
{
    public DateTime Timestamp { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Last { get; set; }
    public int Volume { get; set; }

    // Для отображения изменений
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}

public class ChannelInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PipeName { get; set; } = string.Empty;
    public List<string> Tickers { get; set; } = new();
    public ConsoleColor Color { get; set; }
}
