using Microsoft.Extensions.Logging;
using QuoteGeneratorWorker.Models;

namespace QuoteGeneratorWorker.Handlers;

public class ApiQuoteHandler : IQuoteHandler
{
    private readonly ILogger<ApiQuoteHandler> _logger;

    public ApiQuoteHandler(ILogger<ApiQuoteHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(StockQuote quote, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Имитация отправки по API
        _logger.LogInformation("ApiHandler: Отправка котировки {Symbol} по цене ${Price}", quote.Symbol, quote.Price);
    }
}
