// Services/SecondSubscriberService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class SecondSubscriberService : BackgroundService
{
    private const string PipeName = @"\\.\pipe\second-subscriber-channel";
    private readonly ILogger<SecondSubscriberService> _logger;
    private readonly string[] _subscribedTickers = { "AMZN", "AAPL" };

    public SecondSubscriberService(ILogger<SecondSubscriberService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"SecondSubscriberService: Starting... (Subscribed: {string.Join(", ", _subscribedTickers)})");

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeClientStream? pipeClient = null;
            StreamReader? reader = null;

            try
            {
                pipeClient = new NamedPipeClientStream(
                    ".",
                    "second-subscriber-channel",
                    PipeDirection.In,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"SecondSubscriberService: Connecting to {PipeName}...");
                await pipeClient.ConnectAsync(5000, stoppingToken);
                _logger.LogInformation("SecondSubscriberService: ✅ Connected to QuoteServer!");

                // ✅ Создаём StreamReader ОДИН раз на всё соединение
                reader = new StreamReader(pipeClient);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stoppingToken);
                    if (line == null)
                    {
                        _logger.LogInformation("SecondSubscriberService: Server disconnected.");
                        break;
                    }

                    var quote = JsonSerializer.Deserialize<StockQuote>(line);
                    _logger.LogInformation($"*** SECOND SUBSCRIBER *** 📈 {quote?.Ticker}, Price: {quote?.Last:C}, Volume: {quote?.Volume}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("SecondSubscriberService: Stopping...");
                break;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "SecondSubscriberService: ⏱️ Connection timeout. Retrying in 2s...");
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecondSubscriberService: ❌ Error. Reconnecting in 2s...");
                await Task.Delay(2000, stoppingToken);
            }
            finally
            {
                // ✅ Освобождаем ресурсы
                try { reader?.Dispose(); } catch { }
                try { pipeClient?.Dispose(); } catch { }
            }
        }

        _logger.LogInformation("SecondSubscriberService: Stopped.");
    }
}

