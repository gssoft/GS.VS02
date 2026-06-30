using Microsoft.Extensions.Logging;
using QuoteGeneratorWorker.Models;

namespace QuoteGeneratorWorker.Handlers;

public class DatabaseQuoteHandler : IQuoteHandler
{
    private readonly ILogger<DatabaseQuoteHandler> _logger;

    public DatabaseQuoteHandler(ILogger<DatabaseQuoteHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(StockQuote quote, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Имитация работы с БД
        _logger.LogInformation("DatabaseHandler: Сохранение котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
    }
}
