namespace QuoteModels;

public record StockQuote(string Ticker, decimal Bid, decimal Ask, decimal Last, int Volume);
