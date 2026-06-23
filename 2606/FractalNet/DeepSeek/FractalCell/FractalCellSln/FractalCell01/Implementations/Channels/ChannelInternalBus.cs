// // Implementations/Channels/ChannelInternalBus.cs

using System.Threading.Channels;

public class ChannelInternalBus : InternalBusTemplate
{
    private readonly Channel<IApplicationEvent> _channel;
    private readonly Dictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();

    public ChannelInternalBus(string busId, BusConfiguration config)
        : base(busId, config)
    {
        _channel = Channel.CreateBounded<IApplicationEvent>(config.Capacity);
    }

    public override async Task PublishAsync<TEvent>(TEvent @event)
    {
        await _channel.Writer.WriteAsync(@event);
    }

    public override IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<IApplicationEvent, Task>>();

        var wrappedHandler = new Func<IApplicationEvent, Task>(e =>
            handler((TEvent)e));
        _handlers[eventType].Add(wrappedHandler);

        return new Unsubscriber(() => _handlers[eventType].Remove(wrappedHandler));
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        await foreach (var @event in _channel.Reader.ReadAllAsync(ct))
        {
            if (_handlers.TryGetValue(@event.GetType(), out var handlers))
            {
                await Task.WhenAll(handlers.Select(h => h(@event)));
            }
        }
    }

    public override Task StopAsync()
    {
        _channel.Writer.Complete();
        return Task.CompletedTask;
    }
}
