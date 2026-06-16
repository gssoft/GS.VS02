// Services/FirstSubscriberService.cs
using System.IO.Pipes;
using System.Text.Json;

using NamedPipes.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NamedPipes.Services;

public sealed class FirstSubscriberService(ILogger<FirstSubscriberService> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private const string PipeName = @"\\.\pipe\first-subscriber-channel";
    private readonly ILogger<FirstSubscriberService> logger = logger;
    private readonly IServiceProvider serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(5000, stoppingToken);

        logger.LogInformation("FirstSubscriber connected to pipe.");

        using var reader = new StreamReader(pipeClient);
        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(stoppingToken);
            if (line == null) continue;

            var quote = JsonSerializer.Deserialize<StockQuote>(line);
            logger.LogInformation($"FirstSubscriber received quote: {quote?.Ticker}, Price: {quote?.Last}");
        }
    }
}

