// Поместите этот файл в папку Handlers (создайте её, если нет)
using QuoteGeneratorWorker.Models;

namespace QuoteGeneratorWorker.Handlers;

public interface IQuoteHandler
{
    Task HandleAsync(StockQuote quote, CancellationToken cancellationToken);
}
