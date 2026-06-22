// Шаблон типовой ячейки
public abstract class FractalCellTemplate<TInternalBus, TExternalBus>
    : BackgroundService
    where TInternalBus : IInternalBus
    where TExternalBus : IExternalBus
{
    protected readonly TInternalBus InternalBus;
    protected readonly TExternalBus ExternalBus;
    protected readonly CellConfiguration Configuration;
    protected readonly List<BackgroundService> _backgroundServices = new();

    protected FractalCellTemplate(
        CellConfiguration configuration,
        TInternalBus internalBus,
        TExternalBus externalBus)
    {
        Configuration = configuration;
        InternalBus = internalBus;
        ExternalBus = externalBus;
    }

    // Абстрактный метод для создания BackgroundServices
    protected abstract IEnumerable<BackgroundService> CreateBackgroundServices();

    // Абстрактный метод для конфигурации обработчиков
    protected abstract void ConfigureHandlers();

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Создаем и запускаем все BackgroundServices
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
        await foreach (var @event in ExternalBus.ReadAllAsync(stoppingToken))
        {
            await InternalBus.PublishAsync(@event);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await InternalBus.StopAsync();
        foreach (var bs in _backgroundServices)
        {
            await bs.StopAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}
