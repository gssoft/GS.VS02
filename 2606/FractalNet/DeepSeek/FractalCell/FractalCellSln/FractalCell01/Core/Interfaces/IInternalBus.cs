// Core/Interface/IInternalBus.cs

public interface IInternalBus
{
    string BusId { get; }
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IApplicationEvent;
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent;
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
}
