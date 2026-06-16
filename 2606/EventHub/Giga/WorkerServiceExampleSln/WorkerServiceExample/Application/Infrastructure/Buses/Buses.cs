// Application/Infrastructure/Buses/Buses.cs

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Buses;

public class CommandBus : ICommandBus
{
    private readonly ChannelWriter<ICommand> _writer;

    public CommandBus(ChannelWriter<ICommand> writer)
    {
        _writer = writer;
    }

    public ValueTask SendAsync(ICommand command, CancellationToken ct = default)
    {
        return _writer.WriteAsync(command, ct);
    }
}

public class EventBus : IEventBus
{
    private readonly ChannelWriter<IEvent> _writer;

    public EventBus(ChannelWriter<IEvent> writer)
    {
        _writer = writer;
    }

    public ValueTask PublishAsync(IEvent @event, CancellationToken ct = default)
    {
        return _writer.WriteAsync(@event, ct);
    }
}
