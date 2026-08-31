using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RabbitMqMicroEventBus(string hostName = "localhost", int port = 15672, string username = "guest", string password = "guest")
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
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Fanout, durable: true);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        var eventType = typeof(TEvent);
        var wrappedHandler = new Func<object, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

        _handlers.AddOrUpdate(
            eventType,
            _ => new List<Func<object, CancellationToken, Task>> { wrappedHandler },
            (_, list) => { list.Add(wrappedHandler); return list; });

        // Генерируем уникальное имя очереди для каждого подписчика
        var queueName = $"{eventType.Name}_{Guid.NewGuid():N}";
        _channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true);
        _channel.QueueBind(queue: queueName, exchange: _exchangeName, routingKey: "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var evt = JsonSerializer.Deserialize(json, eventType, _jsonOptions);
            if (evt != null)
            {
                foreach (var h in _handlers.GetOrAdd(eventType, _ => new List<Func<object, CancellationToken, Task>>()).ToList())
                {
                    await h(evt, CancellationToken.None);
                }
            }
            // 26.08.31
           // _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };
        // 26.08.31
        //_channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        // Не используется в текущей архитектуре
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = typeof(TEvent);
        var json = JsonSerializer.Serialize(@event, eventType, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: "",
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
