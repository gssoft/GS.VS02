using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Templates;

public abstract class FractalCellTemplate<TInternalBus, TExternalBus>
    : BackgroundService, IFractalCell
    where TInternalBus : IInternalBus
    where TExternalBus : IExternalBus
{
    protected readonly TInternalBus InternalBusField;
    protected readonly TExternalBus ExternalBusField;
    protected readonly FractalCellConfiguration Configuration;
    protected readonly ILogger Logger;
    protected readonly List<BackgroundService> _backgroundServices = new();

    public string CellId => Configuration.CellId;
    public IInternalBus InternalBus => InternalBusField;
    public IExternalBus ExternalBus => ExternalBusField;

    protected FractalCellTemplate(
        FractalCellConfiguration configuration,
        TInternalBus internalBus,
        TExternalBus externalBus,
        ILogger logger)
    {
        Configuration = configuration;
        InternalBusField = internalBus;
        ExternalBusField = externalBus;
        Logger = logger;
    }

    protected abstract IEnumerable<BackgroundService> CreateBackgroundServices();
    protected abstract void ConfigureHandlers();

    public async Task InitializeAsync()
    {
        ConfigureHandlers();
        await Task.CompletedTask;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

        foreach (var bs in CreateBackgroundServices())
        {
            _backgroundServices.Add(bs);
            await bs.StartAsync(cancellationToken);
        }

        await InternalBusField.StartAsync(cancellationToken);

        // НЕ вызываем base.StartAsync! Запускаем ExecuteAsync вручную
        _ = ExecuteAsync(cancellationToken);

        Logger.LogInformation("FractalCell {CellId} started successfully", Configuration.CellId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

        try
        {
            await foreach (var @event in ExternalBusField.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await InternalBusField.PublishAsync(@event);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing event in cell {CellId}",
                        Configuration.CellId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("FractalCell {CellId} execute loop canceled", Configuration.CellId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FractalCell {CellId} execute loop error", Configuration.CellId);
        }

        Logger.LogInformation("FractalCell {CellId} execute loop finished", Configuration.CellId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

        await InternalBusField.StopAsync();

        foreach (var bs in _backgroundServices)
        {
            await bs.StopAsync(cancellationToken);
        }

        Logger.LogInformation("FractalCell {CellId} stopped", Configuration.CellId);
    }
}

//using FractalCell.Core.Configuration;
//using FractalCell.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCell.Core.Templates;

//public abstract class FractalCellTemplate<TInternalBus, TExternalBus>
//    : BackgroundService, IFractalCell
//    where TInternalBus : IInternalBus
//    where TExternalBus : IExternalBus
//{
//    protected readonly TInternalBus InternalBusField;
//    protected readonly TExternalBus ExternalBusField;
//    protected readonly FractalCellConfiguration Configuration;
//    protected readonly ILogger Logger;
//    protected readonly List<BackgroundService> _backgroundServices = new();

//    public string CellId => Configuration.CellId;
//    public IInternalBus InternalBus => InternalBusField;
//    public IExternalBus ExternalBus => ExternalBusField;

//    protected FractalCellTemplate(
//        FractalCellConfiguration configuration,
//        TInternalBus internalBus,
//        TExternalBus externalBus,
//        ILogger logger)
//    {
//        Configuration = configuration;
//        InternalBusField = internalBus;
//        ExternalBusField = externalBus;
//        Logger = logger;
//    }

//    protected abstract IEnumerable<BackgroundService> CreateBackgroundServices();
//    protected abstract void ConfigureHandlers();

//    public async Task InitializeAsync()
//    {
//        ConfigureHandlers();
//        await Task.CompletedTask;
//    }

//    public override async Task StartAsync(CancellationToken cancellationToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

//        foreach (var bs in CreateBackgroundServices())
//        {
//            _backgroundServices.Add(bs);
//            await bs.StartAsync(cancellationToken);
//        }

//        await InternalBusField.StartAsync(cancellationToken);

//        // НЕ вызываем base.StartAsync! Запускаем ExecuteAsync вручную
//        _ = ExecuteAsync(cancellationToken);

//        Logger.LogInformation("FractalCell {CellId} started successfully", Configuration.CellId);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

//        try
//        {
//            await foreach (var @event in ExternalBusField.ReadAllAsync(stoppingToken))
//            {
//                try
//                {
//                    await InternalBusField.PublishAsync(@event);
//                }
//                catch (Exception ex)
//                {
//                    Logger.LogError(ex, "Error processing event in cell {CellId}",
//                        Configuration.CellId);
//                }
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            Logger.LogInformation("FractalCell {CellId} execute loop canceled", Configuration.CellId);
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "FractalCell {CellId} execute loop error", Configuration.CellId);
//        }

//        Logger.LogInformation("FractalCell {CellId} execute loop finished", Configuration.CellId);
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

//        await InternalBusField.StopAsync();

//        foreach (var bs in _backgroundServices)
//        {
//            await bs.StopAsync(cancellationToken);
//        }

//        Logger.LogInformation("FractalCell {CellId} stopped", Configuration.CellId);
//    }
//}
