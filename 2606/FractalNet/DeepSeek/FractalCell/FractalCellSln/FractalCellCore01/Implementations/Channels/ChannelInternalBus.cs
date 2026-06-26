// Implementations/Channels/ChannelInternalBus.cs

using System.Collections.Concurrent;
using System.Threading.Channels;

using FractalCellCore.Core.Common;
using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;

namespace FractalCellCore.Implementations.Channels;

public class ChannelInternalBus : InternalBusTemplate
{
    private readonly Channel<IApplicationEvent> _channel;
    private readonly ConcurrentDictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();

    public ChannelInternalBus(string busId, BusSettings config)
        : base(busId, config)
    {
        _channel = Channel.CreateBounded<IApplicationEvent>(
            new BoundedChannelOptions(config.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public override async Task PublishAsync<TEvent>(TEvent @event)
    {
        try
        {
            await _channel.Writer.WriteAsync(@event);
        }
        catch (ChannelClosedException)
        {
            // Канал закрыт, игнорируем
        }
    }

    public override IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);

        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<IApplicationEvent, Task>>());

        var wrappedHandler = new Func<IApplicationEvent, Task>(e => handler((TEvent)e));

        lock (handlers)
        {
            handlers.Add(wrappedHandler);
        }

        return new Unsubscriber(() =>
        {
            lock (handlers)
            {
                handlers.Remove(wrappedHandler);
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                }
            }
        });
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var @event in _channel.Reader.ReadAllAsync(ct))
                {
                    var eventType = @event.GetType();

                    if (_handlers.TryGetValue(eventType, out var handlers))
                    {
                        var handlersCopy = handlers.ToList();

                        if (handlersCopy.Count == 1)
                        {
                            await handlersCopy[0](@event);
                        }
                        else if (handlersCopy.Count > 1)
                        {
                            await Task.WhenAll(handlersCopy.Select(h => h(@event)));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
            catch (Exception ex)
            {
                // Логируем ошибку
            }
        }, ct);

        await Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _channel.Writer.Complete();
        return Task.CompletedTask;
    }
}
