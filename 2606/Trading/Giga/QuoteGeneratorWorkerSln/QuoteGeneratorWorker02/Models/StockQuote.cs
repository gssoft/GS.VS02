// Поместите этот файл в папку Models (создайте её, если нет)
namespace QuoteGeneratorWorker.Models;

/// <summary>
/// Котировка финансового инструмента.
/// </summary>
public record StockQuote(string Symbol, decimal Price);


