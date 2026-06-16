using System.IO.Pipes;
using System.Text.Json;
using QuotesServer.Helpers;
using QuotesServer.Interfaces;
using QuotesServer.Models;
using QuotesServer.Services;

namespace QuotesServer.BackgroundServices;

public sealed class QuoteServerService : BackgroundService
{
    private readonly ILogger<QuoteServerService> _logger;
    private readonly EventHub _eventHub;

    private const string FirstChannel = @"\\.\pipe\first-subscriber-channel";
    private const string SecondChannel = @"\\.\pipe\second-subscriber-channel";

    public QuoteServerService(
        ILogger<QuoteServerService> logger,
        EventHub eventHub)
    {
        _logger = logger;
        _eventHub = eventHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuoteServerService: Starting quote generator...");

        _ = RunPipeServerAsync(FirstChannel, "First", stoppingToken);
        _ = RunPipeServerAsync(SecondChannel, "Second", stoppingToken);

        SetupRouting();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);

            _eventHub.Publish(quote.Ticker, serializedData);
            _logger.LogInformation($"QuoteServerService: Published {quote.Ticker} @ {quote.Last:C}");

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task RunPipeServerAsync(string fullPipeName, string channelName, CancellationToken stoppingToken)
    {
        var pipeName = fullPipeName.Replace(@"\\.\pipe\", "");

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipeServer = null;
            ClientConnection? client = null;

            try
            {
                pipeServer = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"QuoteServerService: Waiting for {channelName} connection on {fullPipeName}...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation($"QuoteServerService: ✅ {channelName} client connected!");

                client = new ClientConnection(pipeServer, fullPipeName);
                _eventHub.RegisterClient(fullPipeName, client);

                await MonitorClientAsync(client, fullPipeName, channelName, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"QuoteServerService: {channelName} pipe server shutting down...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"QuoteServerService: ❌ Error in {channelName} pipe server");
                await Task.Delay(1000, stoppingToken);
            }
            finally
            {
                if (client != null)
                {
                    _eventHub.UnregisterClient(fullPipeName, client);
                    client.Dispose();
                }
                pipeServer?.Dispose();
            }
        }
    }

    private async Task MonitorClientAsync(ClientConnection client, string channel, string channelName, CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!client.IsConnected)
                {
                    _logger.LogInformation($"QuoteServerService: {channelName} client connection lost.");
                    break;
                }
                await Task.Delay(500, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение
        }
        finally
        {
            _logger.LogInformation($"QuoteServerService: {channelName} client disconnected from {channel}.");
        }
    }

    private void SetupRouting()
    {
        _eventHub.Subscribe("GOOGL", data => _eventHub.PublishToChannelAsync(FirstChannel, data));
        _eventHub.Subscribe("MSFT", data => _eventHub.PublishToChannelAsync(FirstChannel, data));
        _eventHub.Subscribe("NVDA", data => _eventHub.PublishToChannelAsync(FirstChannel, data));

        _eventHub.Subscribe("AMZN", data => _eventHub.PublishToChannelAsync(SecondChannel, data));
        _eventHub.Subscribe("AAPL", data => _eventHub.PublishToChannelAsync(SecondChannel, data));

        _logger.LogInformation("QuoteServerService: Routing configured");
        _logger.LogInformation($"QuoteServerService: FirstChannel  → GOOGL, MSFT, NVDA");
        _logger.LogInformation($"QuoteServerService: SecondChannel → AMZN, AAPL");
    }
}
