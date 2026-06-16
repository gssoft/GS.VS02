// Services/QuoteServerService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyApp.Services;

public sealed class QuoteServerService(ILogger<QuoteServerService> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IPublisherService publisher;

    public QuoteServerService(IServiceProvider provider)
    {
        publisher = provider.GetRequiredService<EventHub>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Quote Generator...");

        // Настраиваем распределение котировок по именованным каналам
        SetupPublishingMechanism();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);
            publisher.Publish(quote.Ticker, serializedData);
            logger.LogInformation($"Published quote for {quote.Ticker}.");
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void SetupPublishingMechanism()
    {
        // Функции записи в каналы
        Func<string, bool> firstChannelHandler = (string data) =>
        {
            SendToNamedPipe(data, @"\\.\pipe\first-subscriber-channel"); // Канал для FirstSubscriber
            return true;
        };

        Func<string, bool> secondChannelHandler = (string data) =>
        {
            SendToNamedPipe(data, @"\\.\pipe\second-subscriber-channel"); // Канал для SecondSubscriber
            return true;
        };

        // Назначаем подписку на события EventHub
        publisher.Subscribe("GOOGL", firstChannelHandler);
        publisher.Subscribe("MSFT", firstChannelHandler);
        publisher.Subscribe("NVDA", firstChannelHandler);

        publisher.Subscribe("AMZN", secondChannelHandler);
        publisher.Subscribe("AAPL", secondChannelHandler);
    }

    private void SendToNamedPipe(string data, string pipeName)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None);
            pipeClient.Connect(2000);
            using var writer = new StreamWriter(pipeClient);
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (IOException e)
        {
            logger.LogError(e, $"Failed to send data to pipe '{pipeName}'.");
        }
    }
}