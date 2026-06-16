// Hosting/EventSubscriberService.cs

using System.Threading.Channels;
using EventBus.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventBus.Hosting;

/// <summary>
/// Базовый класс для подписчиков. Читает из изолированного канала и вызывает обработчик в своём контексте.
/// </summary>
public abstract class EventSubscriberService<TEvent> : BackgroundService where TEvent : class
{
    private readonly ChannelReader<TEvent> _reader;
    private readonly ILogger _logger;
    private readonly IHandler<TEvent>? _handler;

    protected EventSubscriberService(
        Channel<TEvent> channel,
        ILogger logger,
        IHandler<TEvent>? handler = null)
    {
        _reader = channel.Reader;
        _logger = logger;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscriber {ServiceName} started.", GetType().Name);

        try
        {
            await foreach (var @event in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    if (_handler != null)
                        await _handler.HandleAsync(@event, stoppingToken);
                    else
                        await HandleAsync(@event, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing event {EventType}.", typeof(TEvent).Name);
                    // Ошибка не ломает цикл чтения. Сервис продолжает работать.
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Subscriber {ServiceName} stopped gracefully.", GetType().Name);
        }
    }

    /// <summary>Переопределяйте, если не используете DI-хендлер.</summary>
    protected virtual Task HandleAsync(TEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}
