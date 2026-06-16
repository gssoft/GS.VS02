// Services/SecondSubscriberService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class SecondSubscriberService(ILogger<SecondSubscriberService> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private const string PipeName = @"\\.\pipe\second-subscriber-channel";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(5000, stoppingToken);

        logger.LogInformation("SecondSubscriber connected to pipe.");

        using var reader = new StreamReader(pipeClient);
        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(stoppingToken);
            if (line == null) continue;

            var quote = JsonSerializer.Deserialize<StockQuote>(line);
            logger.LogInformation($"SecondSubscriber received quote: {quote?.Ticker}, Price: {quote?.Last}");
        }
    }
}
