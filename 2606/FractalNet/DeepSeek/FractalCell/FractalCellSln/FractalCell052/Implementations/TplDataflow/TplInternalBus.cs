using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using FractalCell02.Core.Common;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Core.Templates;

namespace FractalCell02.Implementations.TplDataflow;

public class TplInternalBus : InternalBusTemplate
{
    private readonly ActionBlock<IApplicationEvent> _actionBlock;
    private readonly ConcurrentDictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();

    public TplInternalBus(string busId, BusSettings config)
        : base(busId, config)
    {
        _actionBlock = new ActionBlock<IApplicationEvent>(
            async @event => await ProcessEventAsync(@event),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = config.Capacity,
                MaxDegreeOfParallelism = config.MaxParallelism
            });
    }

    private async Task ProcessEventAsync(IApplicationEvent @event)
    {
        if (_handlers.TryGetValue(@event.GetType(), out var handlers))
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

    public override async Task PublishAsync<TEvent>(TEvent @event)
    {
        await _actionBlock.SendAsync(@event);
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

    public override Task StartAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _actionBlock.Complete();
        return _actionBlock.Completion;
    }
}
