using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NamedPipes.Helpers;
using NamedPipes.Models;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class QuoteClientService(ILogger<QuoteClientService> logger) : BackgroundService
{
    private const string PipeName = "stock-quote-pipe";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken); // Задержка перед попыткой соединения

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                logger.LogInformation("CLIENT: Connecting to server...");
                await pipeClient.ConnectAsync(2000, stoppingToken);
                logger.LogInformation("CLIENT: Connected!");

                var readTask = ProcessIncomingAsync(pipeClient, stoppingToken);
                var writeTask = ProcessOutgoingAsync(pipeClient, stoppingToken);

                await Task.WhenAny(readTask, writeTask);
            }
            catch (TimeoutException)
            {
                logger.LogWarning("CLIENT: Connection timeout. Retrying...");
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "CLIENT: Error. Retrying...");
                await Task.Delay(2000, stoppingToken);
            }
        }

        logger.LogInformation("CLIENT: Service stopped.");
    }

    private async Task ProcessOutgoingAsync(Stream stream, CancellationToken token)
    {
        await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
        while (!token.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate("CLI");
            var json = JsonSerializer.Serialize(quote);

            await writer.WriteLineAsync(json.AsMemory(), token);
            logger.LogInformation($"CLIENT [Sent]    -> {quote.Ticker}: Last {quote.Last}");

            await Task.Delay(1000, token);
        }
    }

    private async Task ProcessIncomingAsync(Stream stream, CancellationToken token)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (!token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(token);
            if (line == null) break;

            var quote = JsonSerializer.Deserialize<StockQuote>(line);
            logger.LogInformation($"CLIENT [Received] <- {quote?.Ticker}: Last {quote?.Last} @ Vol {quote?.Volume} @ {quote?.Timestamp:T}");
        }
    }
}
