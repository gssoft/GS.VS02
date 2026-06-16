// Bus/MessageRouter.cs

using System.Reflection;
using System.Threading.Channels;

class MessageRouter : BackgroundService
{
    private readonly ChannelReader<IMessage> _reader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(Channel<IMessage> channel, IServiceProvider serviceProvider, ILogger<MessageRouter> logger)
    {
        _reader = channel.Reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageRouter started processing messages...");

        try
        {
            await foreach (var message in _reader.ReadAllAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested) break;

                await RouteMessageAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("MessageRouter received shutdown signal, finishing current work...");
        }
        finally
        {
            _logger.LogInformation("MessageRouter finished processing all messages.");
        }
    }

    private async Task RouteMessageAsync(IMessage message, CancellationToken ct)
    {
        var messageType = message.GetType();
        var handlerInterface = typeof(IHandler<>).MakeGenericType(messageType);
        var handlers = (IEnumerable<object>)_serviceProvider.GetServices(handlerInterface);

        if (handlers is null)
        {
            _logger.LogCritical($"handlers is NULL: {messageType.Name}");
            return;
        }

        if (!handlers.Any())
        {
            _logger.LogWarning($"No handlers found for message type: {messageType.Name}");
            return;
        }

        var executionMode = ExecutionMode.Parallel; // можно настроить иначе

        await ExecutionManager.ExecuteHandlersAsync(
            handlers,
            message,
            executionMode,
            ct);
    }
}
