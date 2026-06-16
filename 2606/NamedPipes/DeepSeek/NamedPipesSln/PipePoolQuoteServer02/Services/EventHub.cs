// Services/EventHub.cs

using System.Collections.Concurrent;
using QuotesServer.Interfaces;

namespace QuotesServer.Services;

public class EventHub
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
                if (!client.IsConnected)
                {
                    _logger?.LogDebug($"EventHub: Client on '{channel}' is not connected");
                    disconnected.Add(client);
                    continue;
                }

                await client.WriteAsync(message);
            }
            catch
            {
                disconnected.Add(client);
            }
        }

        foreach (var client in disconnected)
        {
            if (clients.Remove(client))
            {
                try { client.Dispose(); } catch { }
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
    }

    public int GetClientCount(string channel)
    {
        return _clients.TryGetValue(channel, out var list) ? list.Count : 0;
    }

}
