// Services/FirstSubscriberService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Models;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class FirstSubscriberService : BackgroundService
{
    private const string PipeName = @"\\.\pipe\first-subscriber-channel";
    private readonly ILogger<FirstSubscriberService> _logger;
    private readonly string[] _subscribedTickers = { "GOOGL", "MSFT", "NVDA" };

    public FirstSubscriberService(ILogger<FirstSubscriberService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"FirstSubscriberService: Starting... (Subscribed: {string.Join(", ", _subscribedTickers)})");

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeClientStream? pipeClient = null;
            StreamReader? reader = null;

            try
            {
                pipeClient = new NamedPipeClientStream(
                    ".",
                    "first-subscriber-channel",
                    PipeDirection.In,
                    PipeOptions.Asynchronous);

                _logger.LogInformation($"FirstSubscriberService: Connecting to {PipeName}...");
                await pipeClient.ConnectAsync(5000, stoppingToken);
                _logger.LogInformation("FirstSubscriberService: ✅ Connected to QuoteServer!");

                // ✅ Создаём StreamReader ОДИН раз на всё соединение
                reader = new StreamReader(pipeClient);

                while (!stoppingToken.IsCancellationRequested)
                {
                    // ✅ Проверка статуса подключения
                    if (!pipeClient.IsConnected)
                    {
                        _logger.LogInformation("FirstSubscriberService: Connection lost to server.");
                        break;
                    }

                    var line = await reader.ReadLineAsync(stoppingToken);
                    if (line == null)
                    {
                        _logger.LogInformation("FirstSubscriberService: Server disconnected.");
                        break;
                    }

                    var quote = JsonSerializer.Deserialize<StockQuote>(line);
                    _logger.LogInformation($"*** FIRST SUBSCRIBER *** 📈 {quote?.Ticker}, Price: {quote?.Last:C}, Volume: {quote?.Volume}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("FirstSubscriberService: Stopping...");
                break;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "FirstSubscriberService: ⏱️ Connection timeout at line {LineNumber}. Retrying in 2s...",
                    new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetFileLineNumber());
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FirstSubscriberService: ❌ Error at line {LineNumber}. Reconnecting in 2s...",
                    new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetFileLineNumber());
                await Task.Delay(2000, stoppingToken);
            }
            finally
            {
                // ✅ Освобождаем ресурсы
                try { reader?.Dispose(); } catch { }
                try { pipeClient?.Dispose(); } catch { }
            }
        }

        _logger.LogInformation("FirstSubscriberService: Stopped.");
    }
}
