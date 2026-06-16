// Services/PublisherService.cs
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerService.Dataflow;
using WorkerService.Models;

namespace WorkerService.Services
{
    public class PublisherService(QuoteChannel channel, ILogger<PublisherService> logger) : BackgroundService
    {
        private readonly QuoteChannel _channel = channel;
        private readonly ILogger<PublisherService> _logger = logger;
        private readonly Random _random = new();

        // TPL Dataflow блок для обработки входящих котировок (от Subscriber)
        private ActionBlock<Quote>? _incomingQuotesBlock;

        private int _publishedCount;
        private int _receivedCount;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Создаем блок для обработки входящих котировок от Subscriber
            _incomingQuotesBlock = new ActionBlock<Quote>(
                quote => ProcessIncomingQuote(quote),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10,
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = stoppingToken,
                    NameFormat = "Publisher-Incoming"
                });

            // Подписываемся на обратный канал
            _channel.SubscriberToPublisher.LinkTo(
                _incomingQuotesBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            _logger.LogInformation("Publisher Service запущен. Ожидаем котировки...");

            return PublishQuotesAsync(stoppingToken);
        }

        private async Task PublishQuotesAsync(CancellationToken stoppingToken)
        {
            var symbols = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA" };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                { 
                    var symbol = symbols[_random.Next(symbols.Length)];
                    var quote = new Quote
                    {
                        Symbol = symbol,
                        Price = 100 + _random.Next(-20, 20) + (decimal)_random.NextDouble(),
                        Timestamp = DateTime.UtcNow,
                        Source = "Publisher"
                    };

                    // Публикуем в основной канал
                    await _channel.PublisherToSubscriber.SendAsync(quote, stoppingToken);

                    // Отправляем копию в мониторинг
                    _channel.MonitoringBroadcast.Post(quote);

                    _publishedCount++;

                    if (_publishedCount % 10 == 0)
                    {
                        _logger.LogInformation("Publisher: опубликовано {Count} котировок", _publishedCount);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при публикации котировки");
                }
            }

            _logger.LogInformation("Publisher завершил публикацию. Всего: {Count}", _publishedCount);
            _channel.PublisherToSubscriber.Complete();
        }

        private void ProcessIncomingQuote(Quote quote)
        {
            _receivedCount++;
            _logger.LogInformation("Publisher получил обратную связь: {Quote} (всего получено: {Count})",
                quote, _receivedCount);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Publisher Service останавливается...");
            _incomingQuotesBlock?.Complete();
            await base.StopAsync(cancellationToken);
        }
    }
}
