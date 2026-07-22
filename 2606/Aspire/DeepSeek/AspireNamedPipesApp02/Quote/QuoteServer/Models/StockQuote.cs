namespace QuoteServer.Models;

public record StockQuote(DateTime Timestamp, string Ticker, decimal Bid, decimal Ask, decimal Last, int Volume);
