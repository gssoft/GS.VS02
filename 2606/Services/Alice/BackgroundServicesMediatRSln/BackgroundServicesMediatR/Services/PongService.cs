using MediatR;

public class PongService : BackgroundService
{
    private readonly IMediator _mediator;
    private readonly ILogger<PongService> _logger;
    private int _counter = 0;

    public PongService(IMediator mediator, ILogger<PongService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _counter++;
            _logger.LogInformation("PongService sending PONG message #{Counter}", _counter);

            // Отправляем сообщение для PingService
            await _mediator.Publish(new PongMessage(_counter), stoppingToken);

            // Ждём 5 секунд перед следующей отправкой (чтобы увидеть разницу в частоте)
            await Task.Delay(5000, stoppingToken);
        }
    }
}

