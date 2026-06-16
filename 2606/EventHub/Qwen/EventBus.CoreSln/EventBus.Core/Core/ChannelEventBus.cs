// Core/ChannelEventBus.cs

using System.Collections.Concurrent;
using System.Threading.Channels;
using EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace EventBus.Core;

public sealed class ChannelEventBus : IEventBus, IAsyncDisposable
{
    private readonly Channel<object> _inboundChannel;

    // Храним не писатели, а делегаты типа Action<object>
    // Это обходит проблему инвариантности ChannelWriter<T>
    private readonly ConcurrentDictionary<Type, List<Action<object>>> _subscribers = new();

    private readonly ILogger<ChannelEventBus> _logger;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _routingTask;
    private bool _disposed;

    public ChannelEventBus(ILogger<ChannelEventBus> logger)
    {
        _logger = logger;
        _inboundChannel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _routingTask = Task.Run(() => RouteMessagesAsync(_shutdownCts.Token), CancellationToken.None);
    }

    public async ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        ThrowIfDisposed();
        await _inboundChannel.Writer.WriteAsync(@event, cancellationToken);
    }

    /// <summary>
    /// Подписка канала сервиса на тип события.
    /// Вызывается в конструкторе BackgroundService.
    /// </summary>
    public void Subscribe<TEvent>(ChannelWriter<TEvent> targetWriter) where TEvent : class
    {
        ThrowIfDisposed();

        // Создаём делегат, который захватывает типизированный writer
        // и безопасно приводит сообщение к нужному типу
        Action<object> writerAction = (msg) =>
        {
            if (msg is TEvent typedEvent)
            {
                // TryWrite — неблокирующий. Если канал полон — возвращает false.
                // Это защищает роутер от медленных подписчиков.
                if (!targetWriter.TryWrite(typedEvent))
                {
                    _logger?.LogWarning("Subscriber buffer full for {EventType}. Message dropped.", typeof(TEvent).Name);
                }
            }
        };

        var list = _subscribers.GetOrAdd(typeof(TEvent), _ => new());

        lock (list)
        {
            // Избегаем дубликатов подписок
            if (!list.Contains(writerAction))
                list.Add(writerAction);
        }
    }

    private async Task RouteMessagesAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var message in _inboundChannel.Reader.ReadAllAsync(ct))
            {
                var messageType = message.GetType();

                if (!_subscribers.TryGetValue(messageType, out var targets))
                    continue;

                // Делаем снимок списка, чтобы не блокировать долгие переборы
                List<Action<object>> snapshot;
                lock (targets) snapshot = targets.ToList();

                foreach (var writerAction in snapshot)
                {
                    try
                    {
                        // Выполняем делегат — запись в целевой канал
                        writerAction(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to route message to subscriber for {EventType}", messageType.Name);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ожидаемое завершение при остановке хоста
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in message routing loop");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _shutdownCts.Cancel();
        _inboundChannel.Writer.Complete();

        try
        {
            await _routingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Игнорируем ожидаемую отмену
        }

        _shutdownCts.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ChannelEventBus));
    }
}

