using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamedPipes.Helpers;
using NamedPipes.Models;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace NamedPipes.Services;

public sealed class QuoteServerService(ILogger<QuoteServerService> logger) : BackgroundService
{
    private const string PipeName = "stock-quote-pipe";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SERVER: Starting waiting for connection...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                logger.LogInformation("SERVER: Waiting for client connection...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                logger.LogInformation("SERVER: Client connected!");

                var readTask = ProcessIncomingAsync(pipeServer, stoppingToken);
                var writeTask = ProcessOutgoingAsync(pipeServer, stoppingToken);

                await Task.WhenAny(readTask, writeTask);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "SERVER: Error in pipe connection. Restarting loop...");
            }
        }

        logger.LogInformation("SERVER: Service stopped.");
    }

    private async Task ProcessOutgoingAsync(Stream stream, CancellationToken token)
    {
        await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
        while (!token.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate("SRV");
            var json = JsonSerializer.Serialize(quote);

            await writer.WriteLineAsync(json.AsMemory(), token);
            logger.LogInformation($"SERVER [Sent]    -> {quote.Ticker}: Last {quote.Last}");

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
            logger.LogInformation($"SERVER [Received] <- {quote?.Ticker}: Last {quote?.Last} Vol {quote?.Volume} @ {quote?.Timestamp:T}");
        }
    }
}