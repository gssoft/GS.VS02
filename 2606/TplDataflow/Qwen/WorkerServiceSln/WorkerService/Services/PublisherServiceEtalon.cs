// Services/PublisherServiceEtalon.cs
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerService.Dataflow;
using WorkerService.Models;

namespace WorkerService.Services
{
    public class PublisherServiceEtalon(QuoteChannel channel, ILogger<PublisherService> logger) : BackgroundService
    {
        private readonly QuoteChannel _channel = channel;
        private readonly ILogger<PublisherService> _logger = logger;
        private readonly Random _random = new();

        // TPL Dataflow блок для обработки входящих котировок (от Subscriber)
        private ActionBlock<Quote>? _incomingQuotesBlock;

        private int _publishedCount;
        private int _receivedCount;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SetupIncomingQuotesProcessing(stoppingToken);
            _logger.LogInformation("Publisher Service запущен. Ожидаем котировки...");

            await RunQuotePublicationLoop(stoppingToken);
        }

        private void SetupIncomingQuotesProcessing(CancellationToken stoppingToken)
        {
            _incomingQuotesBlock = new ActionBlock<Quote>(
                ProcessIncomingQuote,
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10,
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = stoppingToken,
                    NameFormat = "Publisher-Incoming"
                });

            _channel.SubscriberToPublisher.LinkTo(
                _incomingQuotesBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        private void ProcessIncomingQuote(Quote quote)
        {
            _receivedCount++;
            _logger.LogInformation(
                "Publisher получил обратную связь: {Quote} (всего получено: {Count})",
                quote, _receivedCount);
        }

        private async Task RunQuotePublicationLoop(CancellationToken stoppingToken)
        {
            var symbols = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA" };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var quote = GenerateRandomQuote(symbols);
                    await PublishQuoteAsync(quote, stoppingToken);
                    UpdateAndLogPublicationStats();
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Публикация котировок прервана по запросу остановки.");
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

        private Quote GenerateRandomQuote(string[] symbols)
        {
            var symbol = symbols[_random.Next(symbols.Length)];
            return new Quote
            {
                Symbol = symbol,
                Price = 100 + _random.Next(-20, 20) + (decimal)_random.NextDouble(),
                Timestamp = DateTime.UtcNow,
                Source = "Publisher"
            };
        }

        private async Task PublishQuoteAsync(Quote quote, CancellationToken stoppingToken)
        {
            await _channel.PublisherToSubscriber.SendAsync(quote, stoppingToken);
            _channel.MonitoringBroadcast.Post(quote);
        }

        private void UpdateAndLogPublicationStats()
        {
            _publishedCount++;

            if (_publishedCount % 10 == 0)
            {
                _logger.LogInformation("Publisher: опубликовано {Count} котировок", _publishedCount);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Publisher Service останавливается...");
            _incomingQuotesBlock?.Complete();
            await base.StopAsync(cancellationToken);
        }
    }
}

