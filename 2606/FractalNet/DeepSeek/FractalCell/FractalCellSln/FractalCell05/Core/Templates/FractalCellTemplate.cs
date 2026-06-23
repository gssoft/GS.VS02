using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Core.Templates;

public abstract class FractalCellTemplate<TInternalBus, TExternalBus>
    : BackgroundService
    where TInternalBus : IInternalBus
    where TExternalBus : IExternalBus
{
    protected readonly TInternalBus InternalBus;
    protected readonly TExternalBus ExternalBus;
    protected readonly FractalCellConfiguration Configuration;
    protected readonly ILogger Logger;
    protected readonly List<BackgroundService> _backgroundServices = new();

    protected FractalCellTemplate(
        FractalCellConfiguration configuration,
        TInternalBus internalBus,
        TExternalBus externalBus,
        ILogger logger)
    {
        Configuration = configuration;
        InternalBus = internalBus;
        ExternalBus = externalBus;
        Logger = logger;
    }

    protected abstract IEnumerable<BackgroundService> CreateBackgroundServices();
    protected abstract void ConfigureHandlers();

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

        foreach (var bs in CreateBackgroundServices())
        {
            _backgroundServices.Add(bs);
            await bs.StartAsync(cancellationToken);
        }

        await InternalBus.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

        await foreach (var @event in ExternalBus.ReadAllAsync(stoppingToken))
        {
            try
            {
                await InternalBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing event in cell {CellId}",
                    Configuration.CellId);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

        await InternalBus.StopAsync();
        foreach (var bs in _backgroundServices)
        {
            await bs.StopAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}
