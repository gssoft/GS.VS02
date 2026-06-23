// Implementations/Channels/ChannelFractalCell.cs

public class ChannelFractalCell : FractalCellTemplate<ChannelInternalBus, ChannelExternalBus>
{
    private readonly List<ChannelBackgroundService> _services = new();

    public ChannelFractalCell(
        CellConfiguration configuration,
        ChannelInternalBus internalBus,
        ChannelExternalBus externalBus)
        : base(configuration, internalBus, externalBus)
    {
    }

    protected override IEnumerable<BackgroundService> CreateBackgroundServices()
    {
        for (int i = 0; i < Configuration.BackgroundServiceCount; i++)
        {
            var service = new ChannelBackgroundService(
                $"{Configuration.CellId}-worker-{i}",
                InternalBus,
                Logger,
                Configuration);

            _services.Add(service);
            yield return service;
        }
    }

    protected override void ConfigureHandlers()
    {
        InternalBus.Subscribe<FractalEvent>(async fractalEvent =>
        {
            Logger.LogInformation("Channel cell received: {EventType}",
                fractalEvent.EventType);

            // Маршрутизация через ExternalBus
            if (!string.IsNullOrEmpty(fractalEvent.TargetCellId))
            {
                await ExternalBus.SendToCellAsync(
                    fractalEvent.TargetCellId, fractalEvent);
            }
        });
    }
}

public class ChannelBackgroundService : BackgroundService
{
    private readonly string _serviceId;
    private readonly ChannelInternalBus _internalBus;
    private readonly ILogger _logger;
    private readonly CellConfiguration _config;

    public ChannelBackgroundService(
        string serviceId,
        ChannelInternalBus internalBus,
        ILogger logger,
        CellConfiguration config)
    {
        _serviceId = serviceId;
        _internalBus = internalBus;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChannelBackgroundService {ServiceId} started", _serviceId);

        // Подписываемся на события
        var subscription = _internalBus.Subscribe<FractalEvent>(async @event =>
        {
            _logger.LogInformation(
                "Service {ServiceId} processing event {EventId} of type {EventType}",
                _serviceId, @event.EventId, @event.EventType);

            await ProcessEventAsync(@event);
        });

        try
        {
            // Держим сервис живым пока не отменят
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение
        }
    }

    private Task ProcessEventAsync(FractalEvent @event)
    {
        switch (@event.EventType)
        {
            case "ProcessData":
                _logger.LogInformation(
                    "Channel worker {ServiceId} processing data: {Payload}",
                    _serviceId, @event.Payload);
                break;

            case "Heartbeat":
                _logger.LogDebug(
                    "Channel worker {ServiceId} heartbeat from {Source}",
                    _serviceId, @event.SourceCellId);
                break;
        }

        return Task.CompletedTask;
    }
}
