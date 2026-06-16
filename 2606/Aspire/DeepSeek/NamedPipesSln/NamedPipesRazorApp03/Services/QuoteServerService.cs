// Services/QuoteServerService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NamedPipes.Helpers;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;

namespace NamedPipes.Services;

public sealed class QuoteServerService : BackgroundService
{
    private readonly ILogger<QuoteServerService>? logger;
    private readonly IPublisherService? publisher;

    public QuoteServerService(ILogger<QuoteServerService> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.publisher = serviceProvider.GetRequiredService<EventHub>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Quote Generator...");

        // Настраиваем публикацию котировок в правильные каналы
        SetupPublishingMechanism();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate("SRV");
            var serializedData = JsonSerializer.Serialize(quote);
            publisher?.Publish(quote.Ticker, serializedData);
            logger.LogInformation($"Published quote for {quote.Ticker}.");
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void SetupPublishingMechanism()
    {
        // Функция отправки котировок в первый канал
        Func<string, bool> firstChannelHandler = (string data) =>
        {
            SendToNamedPipe(data, @"\\.\pipe\first-subscriber-channel");
            return true;
        };

        // Функция отправки котировок во второй канал
        Func<string, bool> secondChannelHandler = (string data) =>
        {
            SendToNamedPipe(data, @"\\.\pipe\second-subscriber-channel");
            return true;
        };

        // Подписываемся на конкретные тикеры
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


//// Services/QuoteServerService.cs
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//using NamedPipes.Helpers;
//using System.IO.Pipes;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace NamedPipes.Services;

//public sealed class QuoteServerService( ILogger<QuoteServerService> logger, IServiceProvider serviceProvider) : BackgroundService
//{
//    private readonly IPublisherService? publisher;

//    public QuoteServerService(IServiceProvider provider)
//    {
//        publisher = provider.GetRequiredService<EventHub>();
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        logger.LogInformation("Starting Quote Generator...");

//        // Настраиваем публикацию котировок в правильные каналы
//        SetupPublishingMechanism();

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var quote = QuoteGenerator.Generate("SRV");
//            var serializedData = JsonSerializer.Serialize(quote);
//            publisher?.Publish(quote.Ticker, serializedData);
//            logger.LogInformation($"Published quote for {quote.Ticker}.");
//            await Task.Delay(1000, stoppingToken);
//        }
//    }

//    private void SetupPublishingMechanism()
//    {
//        // Функция отправки котировок в первый канал
//        Func<string, bool> firstChannelHandler = (string data) =>
//        {
//            SendToNamedPipe(data, @"\\.\pipe\first-subscriber-channel");
//            return true;
//        };

//        // Функция отправки котировок во второй канал
//        Func<string, bool> secondChannelHandler = (string data) =>
//        {
//            SendToNamedPipe(data, @"\\.\pipe\second-subscriber-channel");
//            return true;
//        };

//        // Подписываемся на конкретные тикеры
//        publisher.Subscribe("GOOGL", firstChannelHandler);
//        publisher.Subscribe("MSFT", firstChannelHandler);
//        publisher.Subscribe("NVDA", firstChannelHandler);

//        publisher.Subscribe("AMZN", secondChannelHandler);
//        publisher.Subscribe("AAPL", secondChannelHandler);
//    }

//    private void SendToNamedPipe(string data, string pipeName)
//    {
//        try
//        {
//            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None);
//            pipeClient.Connect(2000);
//            using var writer = new StreamWriter(pipeClient);
//            writer.WriteLine(data);
//            writer.Flush();
//        }
//        catch (IOException e)
//        {
//            logger.LogError(e, $"Failed to send data to pipe '{pipeName}'.");
//        }
//    }
//}
