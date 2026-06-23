// Core/Templates/FractalCellTemplate.cs

// Шаблон типовой ячейки
public abstract class FractalCellTemplate<TInternalBus, TExternalBus>
    : BackgroundService
    where TInternalBus : IInternalBus
    where TExternalBus : IExternalBus
{
    protected readonly TInternalBus InternalBus;
    protected readonly TExternalBus ExternalBus;
    protected readonly CellConfiguration Configuration;
    protected readonly ILogger Logger;  // ← protected поле
    protected readonly List<BackgroundService> _backgroundServices = new();

    protected FractalCellTemplate(
        CellConfiguration configuration,
        TInternalBus internalBus,
        TExternalBus externalBus,
        ILogger logger)  // ← Добавлен параметр
    {
        Configuration = configuration;
        InternalBus = internalBus;
        ExternalBus = externalBus;
        Logger = logger;  // ← Сохраняем
    }

    protected abstract IEnumerable<BackgroundService> CreateBackgroundServices();
    protected abstract void ConfigureHandlers();

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
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
        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

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

