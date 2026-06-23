// Implementations/TplDataflow/TplInternalBus.cs

using System.Threading.Tasks.Dataflow;

public class TplInternalBus : InternalBusTemplate
{
    private readonly ActionBlock<IApplicationEvent> _actionBlock;
    private readonly Dictionary<Type, List<Func<IApplicationEvent, Task>>> _handlers = new();

    public TplInternalBus(string busId, BusConfiguration config)
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
            await Task.WhenAll(handlers.Select(h => h(@event)));
        }
    }

    public override async Task PublishAsync<TEvent>(TEvent @event)
    {
        await _actionBlock.SendAsync(@event);
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

    public override Task StartAsync(CancellationToken ct)
    {
        // ActionBlock уже работает после создания
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _actionBlock.Complete();
        return _actionBlock.Completion;
    }
}

