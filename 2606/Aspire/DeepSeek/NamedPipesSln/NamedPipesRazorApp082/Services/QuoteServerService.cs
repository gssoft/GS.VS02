// Services/QuoteServerService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Helpers;
using NamedPipes.Interfaces;
using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class QuoteServerService : BackgroundService
{
    private readonly ILogger<QuoteServerService> _logger;
    private readonly IPublisherService _publisher;
    private readonly EventHub _eventHub;

    // ✅ Используем полные имена каналов ВЕЗДЕ
    private const string FirstChannel = @"\\.\pipe\first-subscriber-channel";
    private const string SecondChannel = @"\\.\pipe\second-subscriber-channel";

    public QuoteServerService(
        ILogger<QuoteServerService> logger,
        IPublisherService publisher,
        EventHub eventHub)
    {
        _logger = logger;
        _publisher = publisher;
        _eventHub = eventHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuoteServerService: Starting quote generator...");

        // Запускаем серверы труб для каждого канала
        _ = RunPipeServerAsync(FirstChannel, "First", stoppingToken);
        _ = RunPipeServerAsync(SecondChannel, "Second", stoppingToken);

        // Настраиваем маршрутизацию
        SetupRouting();

        // Генерируем котировки
        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);

            _publisher.Publish(quote.Ticker, serializedData);
            _logger.LogInformation($"QuoteServerService: Published {quote.Ticker} @ {quote.Last:C}");

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task RunPipeServerAsync(string fullPipeName, string channelName, CancellationToken stoppingToken)
    {
        // ✅ Извлекаем имя трубы без префикса для NamedPipeServerStream
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
                    1, // ✅ ТОЛЬКО 1 клиент на канал!
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"QuoteServerService: Waiting for {channelName} connection on {fullPipeName}...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation($"QuoteServerService: ✅ {channelName} client connected!");

                client = new ClientConnection(pipeServer, fullPipeName, _logger);
                _eventHub.RegisterClient(fullPipeName, client);

                // ✅ ЖДЁМ пока клиент не отключится (НЕ fire-and-forget!)
                await MonitorClientAsync(client, fullPipeName, channelName, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"QuoteServerService: {channelName} pipe server shutting down...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"QuoteServerService: ❌ Error in {channelName} pipe server at line {System.Environment.StackTrace}");
                await Task.Delay(1000, stoppingToken);
            }
            finally
            {
                // ✅ Очищаем ресурсы ТОЛЬКО после отключения клиента
                if (client != null)
                {
                    _eventHub.UnregisterClient(fullPipeName, client);
                    client.Dispose();
                }
                pipeServer?.Dispose();

                _logger.LogInformation($"QuoteServerService: {channelName} resources cleaned up.");
            }
        }
    }

    private async Task MonitorClientAsync(ClientConnection client, string channel, string channelName, CancellationToken stoppingToken)
    {
        try
        {
            // ✅ Регулярно проверяем статус подключения
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
            _logger.LogDebug($"QuoteServerService: {channelName} monitoring cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"QuoteServerService: {channelName} monitoring error at line {System.Environment.StackTrace}");
        }
        finally
        {
            _logger.LogInformation($"QuoteServerService: {channelName} client disconnected from {channel}.");
        }
    }

    private void SetupRouting()
    {
        // ✅ Используем полные имена каналов ВЕЗДЕ
        _publisher.Subscribe("GOOGL", data => _eventHub.PublishToChannelAsync(FirstChannel, data));
        _publisher.Subscribe("MSFT", data => _eventHub.PublishToChannelAsync(FirstChannel, data));
        _publisher.Subscribe("NVDA", data => _eventHub.PublishToChannelAsync(FirstChannel, data));

        _publisher.Subscribe("AMZN", data => _eventHub.PublishToChannelAsync(SecondChannel, data));
        _publisher.Subscribe("AAPL", data => _eventHub.PublishToChannelAsync(SecondChannel, data));

        _logger.LogInformation("QuoteServerService: Routing configured");
        _logger.LogInformation($"QuoteServerService: FirstChannel  → GOOGL, MSFT, NVDA");
        _logger.LogInformation($"QuoteServerService: SecondChannel → AMZN, AAPL");
    }
}

