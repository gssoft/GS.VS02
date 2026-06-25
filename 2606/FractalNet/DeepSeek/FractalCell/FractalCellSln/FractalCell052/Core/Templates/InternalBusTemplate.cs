using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;

namespace FractalCell02.Core.Templates;

public abstract class InternalBusTemplate : IInternalBus
{
    protected readonly BusSettings Config;
    public string BusId { get; }

    protected InternalBusTemplate(string busId, BusSettings config)
    {
        BusId = busId;
        Config = config;
    }

    public abstract Task PublishAsync<TEvent>(TEvent @event) where TEvent : IApplicationEvent;
    public abstract IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent;
    public abstract Task StartAsync(CancellationToken ct);
    public abstract Task StopAsync();
}
