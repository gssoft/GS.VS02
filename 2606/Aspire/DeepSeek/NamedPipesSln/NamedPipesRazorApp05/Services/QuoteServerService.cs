using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Helpers;
using NamedPipes.Interfaces;
using NamedPipes.Models;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class QuoteServerService : BackgroundService
{
    private readonly ILogger<QuoteServerService> _logger;
    private readonly IPublisherService _publisher;

    public QuoteServerService(ILogger<QuoteServerService> logger, IPublisherService publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuoteServerService: Starting quote generator...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);

            _publisher.Publish(quote.Ticker, serializedData);
            _logger.LogInformation($"QuoteServerService: Published {quote.Ticker} @ {quote.Last}");

            await Task.Delay(1000, stoppingToken);
        }
    }
}
