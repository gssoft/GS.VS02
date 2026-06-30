// Поместите этот файл в папку Handlers (создайте её, если нет)
using Microsoft.Extensions.Logging;
using QuoteGeneratorWorker.Models;

namespace QuoteGeneratorWorker.Handlers;

public class ConsoleQuoteHandler : IQuoteHandler
{
    private readonly ILogger<ConsoleQuoteHandler> _logger;

    public ConsoleQuoteHandler(ILogger<ConsoleQuoteHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(StockQuote quote, CancellationToken cancellationToken)
    {
        // Имитация асинхронной работы (например, записи в БД или вызова API)
        await Task.Delay(50, cancellationToken);

        // Выводим результат с использованием встроенного логгера
        _logger.LogInformation("ConsoleHandler: Обработана котировка: {Symbol} - ${Price}", quote.Symbol, quote.Price);
    }
}

