namespace MicroPlatform.Core;

/// <summary>
/// Minimal in-process event bus contract. TEvent is any type used as an event -
/// there is no marker interface requirement, matching the "just a record" style
/// used throughout this project.
/// </summary>
public interface IMicroEventBus
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);

    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);

    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
