using MediatR;

public class PingService : BackgroundService
{
    private readonly IMediator _mediator;
    private readonly ILogger<PingService> _logger;
    private int _counter = 0;

    public PingService(IMediator mediator, ILogger<PingService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _counter++;
            _logger.LogInformation("PingService sending PING message #{Counter}", _counter);

            // Отправляем сообщение для PongService
            await _mediator.Publish(new PingMessage(_counter), stoppingToken);

            // Ждём 3 секунды перед следующей отправкой
            await Task.Delay(3000, stoppingToken);
        }
    }
}

