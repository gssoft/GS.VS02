using NamedPipes.Interfaces;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NamedPipes.Services;

public class EventHub : IPublisherService
{
    private readonly Dictionary<string, List<Func<string, Task>>> _handlers = new();
    private readonly ILogger<EventHub>? _logger;

    public EventHub(ILogger<EventHub>? logger = null)
    {
        _logger = logger;
    }

    public void Subscribe(string ticker, Func<string, Task> handler)
    {
        if (!_handlers.ContainsKey(ticker))
            _handlers[ticker] = new List<Func<string, Task>>();

        _handlers[ticker].Add(handler);
        _logger?.LogInformation($"EventHub: Subscriber registered for ticker '{ticker}'");
    }

    public void Publish(string ticker, string message)
    {
        if (_handlers.TryGetValue(ticker, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    // Запускаем асинхронно, но не ждем завершения (fire-and-forget)
                    _ = handler.Invoke(message);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"EventHub: Error invoking handler for ticker '{ticker}'");
                }
            }
        }
    }

    // Helper method для записи в Named Pipe
    public static async Task WriteToPipeAsync(string pipeName, string data, ILogger? logger = null)
    {
        try
        {
            await using var pipeClient = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            await pipeClient.ConnectAsync(2000);

            await using var writer = new StreamWriter(pipeClient);
            await writer.WriteLineAsync(data);
            await writer.FlushAsync();

            logger?.LogDebug($"EventHub: Sent data to pipe '{pipeName}'");
        }
        catch (IOException ex)
        {
            // Нормальная ситуация - подписчик может быть еще не подключен
            logger?.LogDebug(ex, $"EventHub: Pipe '{pipeName}' not available (subscriber offline)");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"EventHub: Error writing to pipe '{pipeName}'");
        }
    }
}