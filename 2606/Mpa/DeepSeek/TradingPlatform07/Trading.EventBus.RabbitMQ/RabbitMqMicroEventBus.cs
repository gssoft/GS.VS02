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

        var queueName = eventType.Name;
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, _exchangeName, routingKey: "");

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
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        throw new NotSupportedException();
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

//using System.Collections.Concurrent;
//using System.Text;
//using System.Text.Json;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using Trading.Core;

//namespace Trading.EventBus.RabbitMQ;

//public class RabbitMqMicroEventBus : IMicroEventBus, IDisposable
//{
//    private readonly IConnection _connection;
//    private readonly IModel _channel;
//    private readonly string _exchangeName = "trading_events";
//    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = new();

//    public RabbitMqMicroEventBus(string hostName = "localhost", int port = 5672, string username = "guest", string password = "guest")
//    {
//        var factory = new ConnectionFactory
//        {
//            HostName = hostName,
//            Port = port,
//            UserName = username,
//            Password = password,
//            DispatchConsumersAsync = true
//        };
//        _connection = factory.CreateConnection();
//        _channel = _connection.CreateModel();
//        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Fanout, durable: true);
//    }

//    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
//    {
//        var eventType = typeof(TEvent);
//        var wrappedHandler = new Func<object, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

//        _handlers.AddOrUpdate(
//            eventType,
//            _ => new List<Func<object, CancellationToken, Task>> { wrappedHandler },
//            (_, list) => { list.Add(wrappedHandler); return list; });

//        // Создаём очередь для этого типа события и привязываем к exchange
//        var queueName = eventType.Name; // Уникальная очередь на тип
//        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
//        _channel.QueueBind(queueName, _exchangeName, routingKey: "");

//        var consumer = new AsyncEventingBasicConsumer(_channel);
//        consumer.Received += async (sender, ea) =>
//        {
//            var body = ea.Body.ToArray();
//            var json = Encoding.UTF8.GetString(body);
//            var evt = JsonSerializer.Deserialize(json, eventType);
//            if (evt != null)
//            {
//                foreach (var h in _handlers.GetOrAdd(eventType, _ => new List<Func<object, CancellationToken, Task>>()).ToList())
//                {
//                    await h(evt, CancellationToken.None);
//                }
//            }
//            _channel.BasicAck(ea.DeliveryTag, multiple: false);
//        };

//        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
//    }

//    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
//    {
//        // Для простоты не реализуем; в реальном проекте нужно удалить обработчик
//        throw new NotSupportedException();
//    }

//    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
//    {
//        var eventType = typeof(TEvent);
//        var json = JsonSerializer.Serialize(@event, eventType);
//        var body = Encoding.UTF8.GetBytes(json);

//        _channel.BasicPublish(
//            exchange: _exchangeName,
//            routingKey: "",
//            basicProperties: null,
//            body: body);

//        return Task.CompletedTask;
//    }

//    public void Dispose()
//    {
//        _channel?.Close();
//        _connection?.Close();
//    }
//}
