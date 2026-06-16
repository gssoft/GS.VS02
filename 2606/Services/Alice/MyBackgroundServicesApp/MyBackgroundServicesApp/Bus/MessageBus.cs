// Bus/MessageBus.cs

using System.Threading.Channels;

class MessageBus : IMessageBus
{
    private readonly Channel<IMessage> _channel;
    private readonly IServiceProvider _serviceProvider;

    public MessageBus(IServiceProvider serviceProvider, Channel<IMessage> channel)
    {
        _serviceProvider = serviceProvider;
        _channel = channel;
    }

    public ValueTask PublishAsync(IMessage message, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(message, ct);
    }

    public ValueTask SendAsync(ICommand command, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(command, ct);
    }
}

