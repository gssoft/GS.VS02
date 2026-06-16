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

    private const string FirstChannel = "first-subscriber-channel";
    private const string SecondChannel = "second-subscriber-channel";

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
            _logger.LogInformation($"QuoteServerService: Published {quote.Ticker} @ {quote.Last}");

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task RunPipeServerAsync(string pipeName, string channelName, CancellationToken stoppingToken)
    {
        var fullPipeName = $@"\\.\pipe\{pipeName}";

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipeServer = null;
            ClientConnection? client = null;

            try
            {
                pipeServer = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.Out,
                    10,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"QuoteServerService: Waiting for {channelName} connection on {fullPipeName}...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogInformation($"QuoteServerService: {channelName} client connected!");

                client = new ClientConnection(pipeServer, fullPipeName, _logger);
                _eventHub.RegisterClient(fullPipeName, client);

                // ✅ ДЕРЖИМ соединение, пока клиент подключен
                await MonitorClientAsync(client, fullPipeName, channelName, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"QuoteServerService: Error in {channelName} pipe server.");
                await Task.Delay(1000, stoppingToken);
            }
            finally
            {
                // ✅ Очищаем ресурсы только после отключения клиента
                client?.Dispose();
                _eventHub.UnregisterClient(fullPipeName, client!);
                _logger.LogInformation($"QuoteServerService: {channelName} resources cleaned up.");
            }
        }
    }

    private async Task MonitorClientAsync(ClientConnection client, string channel, string channelName, CancellationToken stoppingToken)
    {
        try
        {
            // ✅ Ждём, пока клиент действительно не отключится
            while (!stoppingToken.IsCancellationRequested && client.IsConnected)
            {
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
        _publisher.Subscribe("GOOGL", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));
        _publisher.Subscribe("MSFT", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));
        _publisher.Subscribe("NVDA", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));

        _publisher.Subscribe("AMZN", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{SecondChannel}", data));
        _publisher.Subscribe("AAPL", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{SecondChannel}", data));

        _logger.LogInformation("QuoteServerService: Routing configured");
    }
}

//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using NamedPipes.Helpers;
//using NamedPipes.Interfaces;
//using NamedPipes.Models;
//using System.IO.Pipes;
//using System.Text.Json;

//namespace NamedPipes.Services;

//public sealed class QuoteServerService : BackgroundService
//{
//    private readonly ILogger<QuoteServerService> _logger;
//    private readonly IPublisherService _publisher;
//    private readonly EventHub _eventHub;

//    private const string FirstChannel = "first-subscriber-channel";
//    private const string SecondChannel = "second-subscriber-channel";

//    public QuoteServerService(
//        ILogger<QuoteServerService> logger,
//        IPublisherService publisher,
//        EventHub eventHub)
//    {
//        _logger = logger;
//        _publisher = publisher;
//        _eventHub = eventHub;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("QuoteServerService: Starting quote generator...");

//        // Запускаем серверы труб для каждого канала
//        _ = RunPipeServerAsync(FirstChannel, "First", stoppingToken);
//        _ = RunPipeServerAsync(SecondChannel, "Second", stoppingToken);

//        // Настраиваем маршрутизацию
//        SetupRouting();

//        // Генерируем котировки
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var quote = QuoteGenerator.Generate();
//            var serializedData = JsonSerializer.Serialize(quote);

//            _publisher.Publish(quote.Ticker, serializedData);
//            _logger.LogInformation($"QuoteServerService: Published {quote.Ticker} @ {quote.Last}");

//            await Task.Delay(1000, stoppingToken);
//        }
//    }

//    private async Task RunPipeServerAsync(string pipeName, string channelName, CancellationToken stoppingToken)
//    {
//        var fullPipeName = $@"\\.\pipe\{pipeName}";

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                var pipeServer = new NamedPipeServerStream(
//                    pipeName,
//                    PipeDirection.Out,
//                    10, // Максимум 10 клиентов на канал
//                    PipeTransmissionMode.Byte,
//                    PipeOptions.Asynchronous);

//                _logger.LogInformation($"QuoteServerService: Waiting for {channelName} connection on {fullPipeName}...");
//                await pipeServer.WaitForConnectionAsync(stoppingToken);
//                _logger.LogInformation($"QuoteServerService: {channelName} client connected!");

//                var client = new ClientConnection(pipeServer, fullPipeName, _logger);
//                _eventHub.RegisterClient(fullPipeName, client);

//                // Мониторим подключение клиента в фоне
//                _ = MonitorClientAsync(client, fullPipeName, stoppingToken);
//            }
//            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//            {
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"QuoteServerService: Error in {channelName} pipe server.");
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//    }

//    private async Task MonitorClientAsync(IClientConnection client, string channel, CancellationToken stoppingToken)
//    {
//        try
//        {
//            while (!stoppingToken.IsCancellationRequested && client.IsConnected)
//            {
//                await Task.Delay(1000, stoppingToken);
//            }
//        }
//        finally
//        {
//            _eventHub.UnregisterClient(channel, client);
//            _logger.LogInformation($"QuoteServerService: {channel} client disconnected.");
//        }
//    }

//    private void SetupRouting()
//    {
//        // Первый канал: GOOGL, MSFT, NVDA
//        _publisher.Subscribe("GOOGL", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));
//        _publisher.Subscribe("MSFT", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));
//        _publisher.Subscribe("NVDA", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{FirstChannel}", data));

//        // Второй канал: AMZN, AAPL
//        _publisher.Subscribe("AMZN", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{SecondChannel}", data));
//        _publisher.Subscribe("AAPL", data => _eventHub.PublishToChannelAsync($@"\\.\pipe\{SecondChannel}", data));

//        _logger.LogInformation("QuoteServerService: Routing configured");
//    }
//}
