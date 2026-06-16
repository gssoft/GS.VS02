// Abstractions/IEventBus.cs

namespace EventBus.Abstractions;

/// <summary>
/// Медиа-стиль API для публикации событий. 
/// Возвращает управление мгновенно после записи во входной буфер.
/// </summary>
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
