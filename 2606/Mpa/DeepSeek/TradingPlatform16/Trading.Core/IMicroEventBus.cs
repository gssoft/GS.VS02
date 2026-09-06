// IMicroEventBus.cs
namespace Trading.Core;

public interface IMicroEventBus
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
