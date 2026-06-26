// Implementations/Channels/ChannelFractalCell.cs

using FractalCellCore.Core.Templates;
using FractalCellCore.Core.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Implementations.Channels;

public class ChannelBackgroundService : BackgroundService
{
    private readonly string _serviceId;
    private readonly ChannelInternalBus _internalBus;
    private readonly ILogger _logger;
    private readonly FractalCellConfiguration _config;
    private IDisposable? _subscription;

    public ChannelBackgroundService(
        string serviceId,
        ChannelInternalBus internalBus,
        ILogger logger,
        FractalCellConfiguration config)
    {
        _serviceId = serviceId;
        _internalBus = internalBus;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChannelBackgroundService {ServiceId} started", _serviceId);

        _subscription = _internalBus.Subscribe<FractalEvent>(async @event =>
        {
            _logger.LogInformation(
                "Service {ServiceId} processing event {EventId} of type {EventType}",
                _serviceId, @event.EventId, @event.EventType);

            await ProcessEventAsync(@event);
        });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ChannelBackgroundService {ServiceId} stopping", _serviceId);
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

            default:
                _logger.LogWarning("Unknown event type: {EventType}", @event.EventType);
                break;
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _subscription?.Dispose();
        base.Dispose();
    }
}

public class ChannelFractalCell : FractalCellTemplate<ChannelInternalBus, ChannelExternalBus>
{
    private readonly List<ChannelBackgroundService> _services = new();

    public ChannelFractalCell(
        FractalCellConfiguration configuration,
        ChannelInternalBus internalBus,
        ChannelExternalBus externalBus,
        ILogger<ChannelFractalCell> logger)
        : base(configuration, internalBus, externalBus, logger)
    {
    }

    protected override IEnumerable<BackgroundService> CreateBackgroundServices()
    {
        for (int i = 0; i < Configuration.BackgroundServiceCount; i++)
        {
            var service = new ChannelBackgroundService(
                $"{Configuration.CellId}-worker-{i}",
                InternalBusField,
                Logger,
                Configuration);

            _services.Add(service);
            yield return service;
        }
    }

    protected override void ConfigureHandlers()
    {
        InternalBusField.Subscribe<FractalEvent>(async fractalEvent =>
        {
            Logger.LogInformation("Channel cell {CellId} received event: {EventType} from {Source}",
                Configuration.CellId, fractalEvent.EventType, fractalEvent.SourceCellId);

            if (!string.IsNullOrEmpty(fractalEvent.TargetCellId) &&
                fractalEvent.TargetCellId != Configuration.CellId)
            {
                Logger.LogInformation("Forwarding event {EventId} to {TargetCell}",
                    fractalEvent.EventId, fractalEvent.TargetCellId);

                await ExternalBusField.SendToCellAsync(
                    fractalEvent.TargetCellId, fractalEvent);
            }
        });
    }
}
