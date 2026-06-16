using MediatR;

public class PongMessageHandler : INotificationHandler<PongMessage>
{
    private readonly ILogger<PongMessageHandler> _logger;

    public PongMessageHandler(ILogger<PongMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PongMessage notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received PONG message with counter: {Counter}", notification.Counter);
        return Task.CompletedTask;
    }
}
