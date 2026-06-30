namespace QuoteGeneratorWorker.Models;

/// <summary>
/// Котировка финансового инструмента.
/// </summary>
public record StockQuote(string Symbol, decimal Price);
