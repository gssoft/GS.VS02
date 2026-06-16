using System.IO.Pipes;
using System.Text.Json;

namespace QuotesClientA;

class Program
{
    private const string PipeName = @"\\.\pipe\first-subscriber-channel";
    private static readonly string[] _subscribedTickers = { "GOOGL", "MSFT", "NVDA" };

    static async Task Main(string[] args)
    {
        Console.WriteLine($"QuotesClientA: Starting... (Subscribed to: {string.Join(", ", _subscribedTickers)})");

        await RunClientAsync();
    }

    static async Task RunClientAsync()
    {
        while (true)
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

                Console.WriteLine($"QuotesClientA: Connecting to {PipeName}...");
                await pipeClient.ConnectAsync(5000);
                Console.WriteLine("QuotesClientA: ✅ Connected to QuoteServer!");

                reader = new StreamReader(pipeClient);

                while (true)
                {
                    if (!pipeClient.IsConnected)
                    {
                        Console.WriteLine("QuotesClientA: Connection lost to server.");
                        break;
                    }

                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        Console.WriteLine("QuotesClientA: Server disconnected.");
                        break;
                    }

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        var ticker = root.GetProperty("Ticker").GetString();
                        var last = root.GetProperty("Last").GetDecimal();
                        var volume = root.GetProperty("Volume").GetInt32();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[CLIENT A] 📈 {ticker}, Price: {last:C}, Volume: {volume}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing message: {ex.Message}");
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("QuotesClientA: ⏱️ Connection timeout. Retrying in 2s...");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QuotesClientA: ❌ Error: {ex.Message}. Reconnecting in 2s...");
                await Task.Delay(2000);
            }
            finally
            {
                try { reader?.Dispose(); } catch { }
                try { pipeClient?.Dispose(); } catch { }
            }
        }
    }
}
