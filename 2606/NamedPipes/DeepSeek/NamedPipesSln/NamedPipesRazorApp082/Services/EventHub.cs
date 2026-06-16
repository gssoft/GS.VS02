// Services/EventHub.cs

using NamedPipes.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace NamedPipes.Services;

public class EventHub : IPublisherService, ISubscriberService
{
    private readonly ConcurrentDictionary<string, List<Func<string, Task>>> _handlers = new();
    private readonly ConcurrentDictionary<string, List<IClientConnection>> _clients = new();
    private readonly ILogger<EventHub>? _logger;

    public EventHub(ILogger<EventHub>? logger = null)
    {
        _logger = logger;
    }

    public void Subscribe(string ticker, Func<string, Task> handler)
    {
        _handlers.AddOrUpdate(ticker,
            _ => new List<Func<string, Task>> { handler },
            (_, list) => { list.Add(handler); return list; });

        _logger?.LogInformation($"EventHub: Handler registered for ticker '{ticker}'");
    }

    public void RegisterClient(string channel, IClientConnection client)
    {
        _clients.AddOrUpdate(channel,
            _ => new List<IClientConnection> { client },
            (_, list) => { list.Add(client); return list; });

        var count = _clients.TryGetValue(channel, out var list) ? list.Count : 0;
        _logger?.LogInformation($"EventHub: Client registered on channel '{channel}' (Total: {count})");
    }

    public void UnregisterClient(string channel, IClientConnection client)
    {
        if (_clients.TryGetValue(channel, out var list))
        {
            if (list.Remove(client))
            {
                var count = list.Count;
                _logger?.LogInformation($"EventHub: Client unregistered from channel '{channel}' (Remaining: {count})");
            }
        }
    }

    public async Task PublishToChannelAsync(string channel, string message)
    {
        if (!_clients.TryGetValue(channel, out var clients) || clients.Count == 0)
        {
            _logger?.LogDebug($"EventHub: No clients on channel '{channel}', message dropped");
            return;
        }

        var disconnected = new List<IClientConnection>();

        foreach (var client in clients)
        {
            try
            {
                // ✅ Проверка статуса подключения перед записью
                if (!client.IsConnected)
                {
                    _logger?.LogDebug($"EventHub: Client on '{channel}' is not connected");
                    disconnected.Add(client);
                    continue;
                }

                await client.WriteAsync(message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"EventHub: Write failed for client on '{channel}' at line {System.Environment.StackTrace}");
                disconnected.Add(client);
            }
        }

        // ✅ Удаляем отключенных клиентов и освобождаем ресурсы
        foreach (var client in disconnected)
        {
            if (clients.Remove(client))
            {
                try { client.Dispose(); } catch { }
                _logger?.LogInformation($"EventHub: Disconnected client removed from '{channel}'");
            }
        }
    }

    public void Publish(string ticker, string message)
    {
        if (_handlers.TryGetValue(ticker, out var handlers))
        {
            foreach (var handler in handlers)
            {
                _ = handler.Invoke(message);
            }
        }
        else
        {
            _logger?.LogDebug($"EventHub: No handlers for ticker '{ticker}'");
        }
    }

    public int GetClientCount(string channel)
    {
        return _clients.TryGetValue(channel, out var list) ? list.Count : 0;
    }

    // ✅ Метод для очистки неактивных клиентов (можно вызывать периодически)
    public void CleanupDisconnectedClients()
    {
        foreach (var kvp in _clients.ToList())
        {
            var disconnected = kvp.Value.Where(c => !c.IsConnected).ToList();
            foreach (var client in disconnected)
            {
                if (kvp.Value.Remove(client))
                {
                    try { client.Dispose(); } catch { }
                    _logger?.LogInformation($"EventHub: Cleanup removed disconnected client from '{kvp.Key}'");
                }
            }
        }
    }
}
