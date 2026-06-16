using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Interfaces;
using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class FirstSubscriberService : BackgroundService
{
    private const string PipeName = @"\\.\pipe\first-subscriber-channel";
    private readonly ILogger<FirstSubscriberService> _logger;
    private readonly IPublisherService _eventHub;

    public FirstSubscriberService(ILogger<FirstSubscriberService> logger, IPublisherService eventHub)
    {
        _logger = logger;
        _eventHub = eventHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FirstSubscriberService: Starting...");

        // Подписываемся на свои тикеры в EventHub
        var tickers = new[] { "GOOGL", "MSFT", "NVDA" };
        foreach (var ticker in tickers)
        {
            _eventHub.Subscribe(ticker, async (data) => await SendToPipeAsync(data));
        }

        _logger.LogInformation($"FirstSubscriberService: Subscribed to tickers: {string.Join(", ", tickers)}");

        // Запускаем сервер трубы для получения данных
        await RunPipeServerAsync(stoppingToken);
    }

    private async Task RunPipeServerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"FirstSubscriberService: Waiting for connection on {PipeName}...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation("FirstSubscriberService: Client (EventHub) connected!");

                using var reader = new StreamReader(pipeServer);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stoppingToken);
                    if (line == null)
                    {
                        _logger.LogInformation("FirstSubscriberService: Client disconnected.");
                        break;
                    }

                    var quote = JsonSerializer.Deserialize<StockQuote>(line);
                    _logger.LogInformation($"*** FIRST SUBSCRIBER *** Received: {quote?.Ticker}, Price: {quote?.Last}, Volume: {quote?.Volume}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FirstSubscriberService: Error in pipe server loop.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task SendToPipeAsync(string data)
    {
        await EventHub.WriteToPipeAsync(PipeName, data, _logger);
    }
}
