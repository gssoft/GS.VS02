// Google AI Studio

using System.IO.Pipes;
using System.Text.Json;

// Имя канала IPC
const string PipeName = "stock-quote-pipe";

var builder = WebApplication.CreateBuilder(args);

// Регистрируем сервисы. 
// Важно: Server запускается первым или Client должен уметь ждать появления сервера.
// В HostedServices порядок запуска соответствует порядку регистрации.
builder.Services.AddHostedService<QuoteServerService>();
builder.Services.AddHostedService<QuoteClientService>();

var app = builder.Build();

app.MapGet("/", () => "IPC Services are running in background. Watch the Console.");

app.Run();

// ---------------------------------------------------------
// Domain Model
// ---------------------------------------------------------
public record StockQuote(
    DateTime Timestamp,
    string Ticker,
    decimal Bid,
    decimal Ask,
    decimal Last,
    decimal Volume
);

// ---------------------------------------------------------
// Helper для генерации случайных данных
// ---------------------------------------------------------
public static class QuoteGenerator
{
    private static readonly Random Rnd = new();
    private static readonly string[] Tickers = ["GOOGL", "AMZN", "MSFT", "AAPL", "NVDA"];

    public static StockQuote Generate(string role)
    {
        var ticker = Tickers[Rnd.Next(Tickers.Length)];
        var basePrice = (decimal)Rnd.Next(100, 2000) + (decimal)Rnd.NextDouble();
        var spread = (decimal)Rnd.NextDouble() * 2;

        return new StockQuote(
            Timestamp: DateTime.Now,
            Ticker: $"{ticker}_{role}", // Добавляем суффикс, чтобы видеть, кто породил
            Bid: Math.Round(basePrice - spread, 2),
            Ask: Math.Round(basePrice + spread, 2),
            Last: Math.Round(basePrice, 2),
            Volume: Rnd.Next(1, 1000)
        );
    }
}

// ---------------------------------------------------------
// Service 1: Pipe Server (Создает трубу)
// ---------------------------------------------------------
public class QuoteServerService(ILogger<QuoteServerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SERVER: Starting waiting for connection...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Создаем серверный поток
                await using var pipeServer = new NamedPipeServerStream("stock-quote-pipe", // PipeName,
                    PipeDirection.InOut,
                    1, // Max instances
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                logger.LogInformation("SERVER: Waiting for client connection...");
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                logger.LogInformation("SERVER: Client connected!");

                // Запускаем параллельно чтение и запись
                var readTask = ProcessIncomingAsync(pipeServer, stoppingToken);
                var writeTask = ProcessOutgoingAsync(pipeServer, stoppingToken);

                // Ждем завершения любой из задач (обрыв связи или отмена)
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
            if (line == null) break; // Pipe closed

            var quote = JsonSerializer.Deserialize<StockQuote>(line);
            logger.LogInformation($"SERVER [Received] <- {quote?.Ticker}: Vol {quote?.Volume} @ {quote?.Timestamp:T}");
        }
    }
}

// ---------------------------------------------------------
// Service 2: Pipe Client (Подключается к трубе)
// ---------------------------------------------------------
public class QuoteClientService(ILogger<QuoteClientService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Небольшая задержка, чтобы дать Серверу время подняться (для демо в одном процессе)
        await Task.Delay(500, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var pipeClient = new NamedPipeClientStream(
                    ".", // Local server
                    "stock-quote-pipe",
                    // PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                logger.LogInformation("CLIENT: Connecting to server...");

                // Пытаемся подключиться с таймаутом
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
            logger.LogInformation($"CLIENT [Received] <- {quote?.Ticker}: Vol {quote?.Volume} @ {quote?.Timestamp:T}");
        }
    }
}

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

//app.UseRouting();

//app.UseAuthorization();

//app.MapStaticAssets();
//app.MapRazorPages()
//   .WithStaticAssets();

//app.Run();
