using System.Net.Sockets;
using System.Text.Json;

var serverHost = Environment.GetEnvironmentVariable("SERVER_HOST") ?? "localhost";
var serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "5555");
var channel = "tech"; // для QuoteClient2 измените на "finance"

Console.WriteLine($"Connecting to {serverHost}:{serverPort} for channel '{channel}'...");

using var client = new TcpClient();
await client.ConnectAsync(serverHost, serverPort);

using var stream = client.GetStream();
using var writer = new StreamWriter(stream) { AutoFlush = true };
using var reader = new StreamReader(stream);

// Отправляем имя канала
await writer.WriteLineAsync(channel);

var expectedTickers = new HashSet<string>(channel switch
{
    "tech" => new[] { "GOOGL", "MSFT", "NVDA" },
    "consumer" => new[] { "AMZN", "AAPL" },
    "finance" => new[] { "JPM", "BAC", "GS" },
    "energy" => new[] { "XOM", "CVX" },
    _ => Array.Empty<string>()
});

Console.WriteLine($"Subscribed to {channel}. Waiting for quotes...");

while (true)
{
    var line = await reader.ReadLineAsync();
    if (line == null) break;

    try
    {
        using JsonDocument doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        var ticker = root.GetProperty("Ticker").GetString()!;
        var last = root.GetProperty("Last").GetDecimal();
        var volume = root.GetProperty("Volume").GetInt32();

        if (expectedTickers.Contains(ticker))
        {
            Console.ForegroundColor = channel switch
            {
                "tech" => ConsoleColor.Green,
                "consumer" => ConsoleColor.Yellow,
                "finance" => ConsoleColor.Cyan,
                "energy" => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
            Console.WriteLine($"[{channel}] {ticker} @ {last:C} (Vol: {volume})");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"UNEXPECTED: {ticker}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Parse error: {ex.Message}");
    }
}
