using MediatR;

public class PingMessageHandler : INotificationHandler<PingMessage>
{
    private readonly ILogger<PingMessageHandler> _logger;

    public PingMessageHandler(ILogger<PingMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PingMessage notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received PING message with counter: {Counter}", notification.Counter);
        return Task.CompletedTask;
    }
}

