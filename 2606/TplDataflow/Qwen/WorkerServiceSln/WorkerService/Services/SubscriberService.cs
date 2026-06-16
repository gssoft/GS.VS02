// Services/SubscriberService.cs
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerService.Dataflow;
using WorkerService.Models;

namespace WorkerService.Services
{
    public class SubscriberService(QuoteChannel channel, ILogger<SubscriberService> logger) : BackgroundService
    {
        private readonly QuoteChannel _channel = channel;
        private readonly ILogger<SubscriberService> _logger = logger;

        // TPL Dataflow блоки для обработки
        private TransformBlock<Quote, Quote>? _validationBlock;
        private TransformBlock<Quote, Quote>? _enrichmentBlock;
        private ActionBlock<Quote>? _processingBlock;
        private ActionBlock<Quote>? _responseBlock;

        private int _receivedCount;
        private int _processedCount;
        private int _rejectedCount;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Создаем конвейер обработки: Validation -> Enrichment -> Processing
            _validationBlock = new TransformBlock<Quote, Quote>(
                transform: quote => ValidateQuote(quote),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 20,
                    MaxDegreeOfParallelism = 3,
                    CancellationToken = stoppingToken,
                    NameFormat = "Subscriber-Validation"
                });

            _enrichmentBlock = new TransformBlock<Quote, Quote>(
                quote => EnrichQuote(quote),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 20,
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = stoppingToken,
                    NameFormat = "Subscriber-Enrichment"
                });

            _processingBlock = new ActionBlock<Quote>(
                quote => ProcessQuote(quote),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10,
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = stoppingToken,
                    NameFormat = "Subscriber-Processing"
                });

            // Блок для отправки обратной связи Publisher'у
            _responseBlock = new ActionBlock<Quote>(
                async quote => await SendResponseAsync(quote, stoppingToken),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = stoppingToken,
                    NameFormat = "Subscriber-Response"
                });

            // Соединяем блоки в конвейер
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _validationBlock.LinkTo(_enrichmentBlock, linkOptions);
            _enrichmentBlock.LinkTo(_processingBlock,
                linkOptions,
                quote => quote != null); // Только валидные

            // Подписываемся на канал Publisher'а
            _channel.PublisherToSubscriber.LinkTo(_validationBlock, linkOptions);

            _logger.LogInformation("Subscriber Service запущен. Ожидаем котировки...");

            return Task.CompletedTask;
        }

        private Quote? ValidateQuote(Quote quote)
        {
            _receivedCount++;

            // Имитация валидации (5% котировок отбрасываем)
            if (quote.Price <= 0)
            {
                _rejectedCount++;
                _logger.LogWarning("Отклонена котировка: {Quote}", quote);
                return null;
            }

            if (_receivedCount % 20 == 0)
            {
                _logger.LogInformation("Subscriber: получено {Count} котировок, отклонено {Rejected}",
                    _receivedCount, _rejectedCount);
            }

            return quote;
        }

        private Quote EnrichQuote(Quote quote)
        {
            // Добавляем дополнительную информацию
            quote.Price = Math.Round(quote.Price, 2);
            return quote;
        }

        private void ProcessQuote(Quote quote)
        {
            _processedCount++;

            // Имитация обработки (анализ, сохранение в БД и т.д.)
            _logger.LogDebug("Обработана котировка: {Quote}", quote);

            // Каждую 5-ю котировку отправляем как обратную связь
            if (_processedCount % 5 == 0)
            {
                var response = new Quote
                {
                    Symbol = quote.Symbol,
                    Price = quote.Price * 1.01m, // Небольшая наценка
                    Timestamp = DateTime.UtcNow,
                    Source = "Subscriber-Response"
                };
                _ = _responseBlock?.Post(response);
            }
        }

        private async Task SendResponseAsync(Quote quote, CancellationToken token)
        {
            try
            {
                await _channel.SubscriberToPublisher.SendAsync(quote, token);
                _logger.LogDebug("Отправлена обратная связь: {Quote}", quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке обратной связи");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscriber Service останавливается. Обработано: {Count}", _processedCount);

            _validationBlock?.Complete();
            _enrichmentBlock?.Complete();
            _processingBlock?.Complete();
            _responseBlock?.Complete();

            await Task.WhenAll(
                _validationBlock?.Completion ?? Task.CompletedTask,
                _enrichmentBlock?.Completion ?? Task.CompletedTask,
                _processingBlock?.Completion ?? Task.CompletedTask,
                _responseBlock?.Completion ?? Task.CompletedTask
            );

            await base.StopAsync(cancellationToken);
        }
    }
}
