// Abstractions/IHandler.cs

namespace EventBus.Abstractions;

/// <summary>
/// Интерфейс обработчика события. Выполняется строго в контексте подписчика.
/// </summary>
public interface IHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
