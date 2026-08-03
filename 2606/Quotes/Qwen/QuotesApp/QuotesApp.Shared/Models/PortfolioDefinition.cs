namespace QuotesApp.Shared.Models;

public static class PortfolioDefinition
{
    public static readonly Dictionary<string, PortfolioConfig> Portfolios = new()
    {
        ["tech"] = new PortfolioConfig
        {
            DisplayName = "TECH STOCKS",
            Tickers = new List<string> { "GOOGL", "MSFT", "NVDA" },
            Color = "Green"
        },
        ["consumer"] = new PortfolioConfig
        {
            DisplayName = "CONSUMER STOCKS",
            Tickers = new List<string> { "AMZN", "AAPL" },
            Color = "Yellow"
        },
        ["finance"] = new PortfolioConfig
        {
            DisplayName = "FINANCE STOCKS",
            Tickers = new List<string> { "JPM", "BAC", "GS" },
            Color = "Cyan"
        },
        ["energy"] = new PortfolioConfig
        {
            DisplayName = "ENERGY STOCKS",
            Tickers = new List<string> { "XOM", "CVX" },
            Color = "Red"
        }
    };
}

public class PortfolioConfig
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Tickers { get; set; } = new();
    public string Color { get; set; } = "White";
}
