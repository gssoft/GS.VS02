using System.IO.Pipes;
using System.Text.Json;

namespace QuotesClient;

class Program
{
    private static readonly Dictionary<string, ChannelInfo> Channels = new()
    {
        ["tech"] = new()
        {
            PipeName = "tech-stocks",
            DisplayName = "TECH STOCKS",
            Color = ConsoleColor.Green,
            Tickers = new[] { "GOOGL", "MSFT", "NVDA" }
        },
        ["consumer"] = new()
        {
            PipeName = "consumer-stocks",
            DisplayName = "CONSUMER STOCKS",
            Color = ConsoleColor.Yellow,
            Tickers = new[] { "AMZN", "AAPL" }
        },
        ["finance"] = new()
        {
            PipeName = "finance-stocks",
            DisplayName = "FINANCE STOCKS",
            Color = ConsoleColor.Cyan,
            Tickers = new[] { "JPM", "BAC", "GS" }
        },
        ["energy"] = new()
        {
            PipeName = "energy-stocks",
            DisplayName = "ENERGY STOCKS",
            Color = ConsoleColor.Magenta,
            Tickers = new[] { "XOM", "CVX" }
        }
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("Available channels:");
        foreach (var channel in Channels)
        {
            Console.ForegroundColor = channel.Value.Color;
            Console.WriteLine($"  {channel.Key}: {channel.Value.DisplayName} ({string.Join(", ", channel.Value.Tickers)})");
            Console.ResetColor();
        }

        string? selection = "finance"; // Для теста выбираем finance
        if (args.Length > 0)
        {
            selection = args[0].ToLower();
        }
        else
        {
            Console.Write("\nSelect channel to subscribe (or press Enter for finance): ");
            var input = Console.ReadLine()?.ToLower();
            if (!string.IsNullOrEmpty(input))
            {
                selection = input;
            }
        }

        if (selection == null || !Channels.TryGetValue(selection, out var selectedChannel))
        {
            Console.WriteLine($"Invalid channel '{selection}'. Using default: finance");
            selectedChannel = Channels["finance"];
        }

        Console.ForegroundColor = selectedChannel.Color;
        Console.WriteLine($"\n📡 Connecting to {selectedChannel.DisplayName} channel...");
        Console.WriteLine($"Expecting tickers: {string.Join(", ", selectedChannel.Tickers)}");
        Console.ResetColor();

        await RunClientAsync(selectedChannel);
    }

    static async Task RunClientAsync(ChannelInfo channel)
    {
        var clientId = Guid.NewGuid().ToString().Substring(0, 4);
        var messageCount = 0;
        var expectedTickers = new HashSet<string>(channel.Tickers);

        while (true)
        {
            NamedPipeClientStream? pipeClient = null;
            StreamReader? reader = null;

            try
            {
                // Пытаемся подключиться к любому доступному экземпляру сервера
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        var pipeName = $"{channel.PipeName}_{i}";
                        Console.WriteLine($"[Client {clientId}] Attempting to connect to {pipeName}...");

                        pipeClient = new NamedPipeClientStream(
                            ".",
                            pipeName,
                            PipeDirection.In,
                            PipeOptions.Asynchronous);

                        await pipeClient.ConnectAsync(2000);
                        Console.WriteLine($"[Client {clientId}] ✅ Connected to {pipeName}!");
                        break;
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"[Client {clientId}] ⏱️ Timeout on instance {i}, trying next...");
                        continue;
                    }
                }

                if (pipeClient == null || !pipeClient.IsConnected)
                {
                    throw new TimeoutException("No available server instances");
                }

                Console.ForegroundColor = channel.Color;
                Console.WriteLine($"[Client {clientId}] ✅ Connected to {channel.DisplayName}!");
                Console.ResetColor();

                reader = new StreamReader(pipeClient);

                while (true)
                {
                    if (!pipeClient.IsConnected)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Client {clientId}] ❌ Connection lost");
                        Console.ResetColor();
                        break;
                    }

                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[Client {clientId}] ⚠️ Server disconnected");
                        Console.ResetColor();
                        break;
                    }

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        var ticker = root.GetProperty("Ticker").GetString() ?? "UNKNOWN";
                        var last = root.GetProperty("Last").GetDecimal();
                        var volume = root.GetProperty("Volume").GetInt32();

                        messageCount++;

                        // Проверяем, ожидаемый ли это тикер
                        if (expectedTickers.Contains(ticker))
                        {
                            Console.ForegroundColor = channel.Color;
                            Console.WriteLine($"[Client {clientId}] 📈 [{messageCount}] {ticker}, Price: {last:C}, Volume: {volume}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Client {clientId}] ⚠️ UNEXPECTED TICKER: {ticker} (expected {string.Join(",", expectedTickers)})");
                        }
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Client {clientId}] ❌ Error parsing message: {ex.Message}");
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Client {clientId}] ⏱️ No available servers. Retrying in 2s...");
                Console.ResetColor();
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Client {clientId}] ❌ Error: {ex.Message}. Reconnecting in 2s...");
                Console.ResetColor();
                await Task.Delay(2000);
            }
            finally
            {
                try { reader?.Dispose(); } catch { }
                try { pipeClient?.Dispose(); } catch { }
            }
        }
    }

    class ChannelInfo
    {
        public string PipeName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ConsoleColor Color { get; set; }
        public string[] Tickers { get; set; } = Array.Empty<string>();
    }
}
