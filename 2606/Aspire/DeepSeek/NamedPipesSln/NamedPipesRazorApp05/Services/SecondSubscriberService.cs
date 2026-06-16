using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Interfaces;
using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class SecondSubscriberService : BackgroundService
{
    private const string PipeName = @"\\.\pipe\second-subscriber-channel";
    private readonly ILogger<SecondSubscriberService> _logger;
    private readonly IPublisherService _eventHub;

    public SecondSubscriberService(ILogger<SecondSubscriberService> logger, IPublisherService eventHub)
    {
        _logger = logger;
        _eventHub = eventHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SecondSubscriberService: Starting...");

        // Подписываемся на свои тикеры в EventHub
        var tickers = new[] { "AMZN", "AAPL" };
        foreach (var ticker in tickers)
        {
            _eventHub.Subscribe(ticker, async (data) => await SendToPipeAsync(data));
        }

        _logger.LogInformation($"SecondSubscriberService: Subscribed to tickers: {string.Join(", ", tickers)}");

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

                _logger.LogInformation($"SecondSubscriberService: Waiting for connection on {PipeName}...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation("SecondSubscriberService: Client (EventHub) connected!");

                using var reader = new StreamReader(pipeServer);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stoppingToken);
                    if (line == null)
                    {
                        _logger.LogInformation("SecondSubscriberService: Client disconnected.");
                        break;
                    }

                    var quote = JsonSerializer.Deserialize<StockQuote>(line);
                    _logger.LogInformation($"*** SECOND SUBSCRIBER *** Received: {quote?.Ticker}, Price: {quote?.Last}, Volume: {quote?.Volume}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecondSubscriberService: Error in pipe server loop.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task SendToPipeAsync(string data)
    {
        await EventHub.WriteToPipeAsync(PipeName, data, _logger);
    }
}