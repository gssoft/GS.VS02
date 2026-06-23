// Core/Templates/InternalBusTemplate.cs

// Шаблон внутренней шины
public abstract class InternalBusTemplate : IInternalBus
{
    protected readonly BusConfiguration Config;
    public string BusId { get; }

    protected InternalBusTemplate(string busId, BusConfiguration config)
    {
        BusId = busId;
        Config = config;
    }

    public abstract Task PublishAsync<TEvent>(TEvent @event) where TEvent : IApplicationEvent;
    public abstract IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent;
    public abstract Task StartAsync(CancellationToken ct);
    public abstract Task StopAsync();
}
