using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Trading.Core;

namespace Trading.EventBus.RabbitMQ;

public class RabbitMqMicroEventBus : IMicroEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = "trading_events";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMqMicroEventBus(string hostName = "localhost", int port = 5672, string username = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = username,
            Password = password,
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 1. Используем Direct обменник вместо Fanout
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var eventType = typeof(TEvent);
        var wrappedHandler = new Func<object, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

        var queueName = $"{eventType.Name}_{Guid.NewGuid():N}";
        _channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true);

        // 2. Очередь слушает только события своего типа
        _channel.QueueBind(queue: queueName, exchange: _exchangeName, routingKey: eventType.Name);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var evt = JsonSerializer.Deserialize(json, eventType, _jsonOptions);

            if (evt != null)
            {
                // 3. Вызываем только конкретный обработчик для конкретной очереди
                await wrappedHandler(evt, CancellationToken.None);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        // Можно не реализовывать для текущей архитектуры
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = typeof(TEvent);
        var json = JsonSerializer.Serialize(@event, eventType, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: eventType.Name, // 4. Публикуем событие с указанием его типа
            basicProperties: null,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
