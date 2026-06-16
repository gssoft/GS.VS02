namespace MyApp.Models;

public record StockQuote(DateTime Timestamp, string Ticker, decimal Bid, decimal Ask, decimal Last, decimal Volume);

