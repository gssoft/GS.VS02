// RabbitMqMicroEventBus.cs
using MicroPlatform.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace MicroPlatform.Infrastructure;

public class RabbitMqMicroEventBus : IMicroEventBus, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqMicroEventBus> _logger;
    private readonly string _exchangeName = "micro-events";
    private readonly ActivitySource _activitySource = new("MicroEventBus");
    private IConnection? _connection;
    private IModel? _consumerChannel;
    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = new();
    private readonly object _lock = new();

    //    Копировать
    // Настройки повторной обработки
    private readonly int _maxRetryCount;
    private readonly TimeSpan _retryDelay;
    private readonly bool _useDeadLetter;

    public RabbitMqMicroEventBus(IConnectionFactory connectionFactory,
                                 ILogger<RabbitMqMicroEventBus> logger,
                                 int maxRetryCount = 5,
                                 int retryDelayMs = 2000,
                                 bool useDeadLetter = true)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _maxRetryCount = maxRetryCount;
        _retryDelay = TimeSpan.FromMilliseconds(retryDelayMs);
        _useDeadLetter = useDeadLetter;
        InitializeConnection();
    }

    private void InitializeConnection()
    {
        lock (_lock)
        {
            _connection?.Dispose();
            _connection = _connectionFactory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.ConnectionBlocked += (_, _) => _logger.LogWarning("RabbitMQ connection blocked");
            _connection.ConnectionUnblocked += (_, _) => _logger.LogInformation("RabbitMQ connection unblocked");

            _logger.LogInformation("RabbitMQ connection established");
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText}", e.ReplyText);
        // Попытаемся переподключиться
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            try
            {
                InitializeConnection();
                // Пересоздаём каналы и подписки для всех известных событий
                RebindAllHandlers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection failed, will retry later");
                // Можно добавить экспоненциальную задержку
            }
        });
    }

    private void RebindAllHandlers()
    {
        lock (_lock)
        {
            _consumerChannel?.Dispose();
            _consumerChannel = _connection!.CreateModel();
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += OnMessageReceived;

            foreach (var eventType in _handlers.Keys)
            {
                var queueName = GetQueueName(eventType);
                var dlqName = $"{queueName}_dead";
                var retryExchangeName = $"retry-{eventType.Name}";

                // Объявляем основную очередь с привязкой к DLX
                DeclareQueueWithDeadLetter(_consumerChannel, queueName, dlqName);

                // Привязываем очередь к обменнику
                _consumerChannel.QueueBind(queueName, _exchangeName, eventType.Name);

                // Подписываемся на очередь
                _consumerChannel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Rebound {QueueName} to {ExchangeName}", queueName, _exchangeName);
            }
        }
    }

    private void DeclareQueueWithDeadLetter(IModel channel, string queueName, string dlqName)
    {
        var args = new Dictionary<string, object?>
    {
        { "x-dead-letter-exchange", _exchangeName },
        { "x-dead-letter-routing-key", dlqName }
    };
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        // Dead-letter очередь (обычная, без DLX)
        channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, _exchangeName, dlqName);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var eventType = typeof(TEvent);
        _handlers.AddOrUpdate(eventType,
            _ => new List<Func<object, CancellationToken, Task>> { (evt, ct) => handler((TEvent)evt, ct) },
            (_, list) => { list.Add((evt, ct) => handler((TEvent)evt, ct)); return list; });

        lock (_lock)
        {
            if (_connection == null || !_connection.IsOpen)
                InitializeConnection();

            _consumerChannel ??= _connection!.CreateModel();
            var queueName = GetQueueName(eventType);
            DeclareQueueWithDeadLetter(_consumerChannel, queueName, $"{queueName}_dead");
            _consumerChannel.QueueBind(queueName, _exchangeName, eventType.Name);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += OnMessageReceived;
            _consumerChannel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        var eventTypeName = ea.RoutingKey;
        using var activity = _activitySource.StartActivity($"Handle {eventTypeName}", ActivityKind.Consumer);
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);
        var channel = (IModel)sender;

        try
        {
            // Извлекаем тип события
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == eventTypeName);
            if (eventType == null)
            {
                _logger.LogWarning("Unknown event type {EventType}", eventTypeName);
                channel.BasicNack(ea.DeliveryTag, false, false); // Не перекладываем неизвестные события
                return;
            }

            var @event = JsonSerializer.Deserialize(json, eventType);
            if (@event == null) return;

            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    await handler(@event, CancellationToken.None);
                }
            }
            // Всё успешно — подтверждаем
            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventTypeName);

            // Извлекаем счётчик попыток из заголовков
            int retryCount = 0;
            if (ea.BasicProperties?.Headers?.TryGetValue("x-retry-count", out var retryObj) == true)
                retryCount = Convert.ToInt32(retryObj);

            if (retryCount < _maxRetryCount)
            {
                // Увеличиваем счётчик и переотправляем в ту же очередь с задержкой
                var newHeaders = ea.BasicProperties?.Headers ?? new Dictionary<string, object?>();
                newHeaders["x-retry-count"] = retryCount + 1;
                var properties = channel.CreateBasicProperties();
                properties.Headers = newHeaders;
                properties.Persistent = true;

                // Публикуем заново через dead-letter exchange (или retry-exchange с TTL)
                // Для простоты используем задержку на уровне публикации, но лучше через отдельный retry exchange с per-message TTL
                // Здесь показываем публикацию в dead-letter очередь с задержкой (через TTL)
                channel.BasicPublish(exchange: "", routingKey: $"{ea.RoutingKey}_dead", basicProperties: properties, body: body);
                // Подтверждаем оригинал, так как он был переложен
                channel.BasicAck(ea.DeliveryTag, false);
            }
            else
            {
                // Исчерпаны попытки — отправляем в dead-letter очередь (или просто логируем)
                _logger.LogError("Max retries exceeded for event {EventType}, sending to DLQ", eventTypeName);
                // В нашем случае dead-letter очередь уже объявлена, просто отбрасываем сообщение (nack без requeue)
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity($"Publish {typeof(TEvent).Name}", ActivityKind.Producer);
        var eventType = typeof(TEvent);
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        lock (_lock)
        {
            if (_connection == null || !_connection.IsOpen)
                InitializeConnection();
        }

        using var channel = _connection!.CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object?> { { "x-retry-count", 0 } }; // начальный счётчик

        channel.BasicPublish(exchange: _exchangeName, routingKey: eventType.Name, mandatory: true, basicProperties: properties, body: body);
        _logger.LogInformation("Published {EventType}", eventType.Name);
        await Task.CompletedTask;
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
            list.Remove((evt, ct) => handler((TEvent)evt, ct));
    }

    private static string GetQueueName(Type eventType) => eventType.Name;

    public void Dispose()
    {
        _consumerChannel?.Dispose();
        _connection?.Dispose();
    }

}
