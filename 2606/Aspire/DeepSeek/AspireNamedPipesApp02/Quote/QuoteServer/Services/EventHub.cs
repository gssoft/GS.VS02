// Services/EventHub.cs
using System.Collections.Concurrent;
using System.Text.Json;
using QuoteServer.Models;

namespace QuoteServer.Services;

public class EventHub
{
    private readonly ConcurrentDictionary<string, List<StreamWriter>> _channelClients = new();
    private readonly ILogger<EventHub> _logger;

    public EventHub(ILogger<EventHub> logger)
    {
        _logger = logger;
    }

    public void RegisterClient(string channel, StreamWriter writer)
    {
        _channelClients.AddOrUpdate(channel,
            _ => new List<StreamWriter> { writer },
            (_, list) => { list.Add(writer); return list; });

        _logger.LogInformation("Client registered on channel '{Channel}' (total: {Count})", channel, _channelClients[channel].Count);
    }

    public void UnregisterClient(string channel, StreamWriter writer)
    {
        if (_channelClients.TryGetValue(channel, out var list))
        {
            list.Remove(writer);
            _logger.LogInformation("Client left channel '{Channel}'", channel);
        }
    }

    public async Task PublishToChannelAsync(string channel, StockQuote quote, CancellationToken token)
    {
        if (!_channelClients.TryGetValue(channel, out var clients)) return;

        string json = JsonSerializer.Serialize(quote) + "\n";
        var deadClients = new List<StreamWriter>();

        foreach (var writer in clients)
        {
            try
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync(token);
            }
            catch
            {
                deadClients.Add(writer);
            }
        }

        foreach (var dead in deadClients)
        {
            clients.Remove(dead);
            try { dead.Dispose(); } catch { }
        }
    }
}