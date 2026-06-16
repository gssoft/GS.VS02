// Services/FirstSubscriberService.cs
using System.IO.Pipes;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MyApp.Models;


namespace MyApp.Services;

public sealed class FirstSubscriberService(ILogger<FirstSubscriberService> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private const string PipeName = @"\\.\pipe\first-subscriber-channel"; // Имя канала для первого потребителя

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(2000, stoppingToken);

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
