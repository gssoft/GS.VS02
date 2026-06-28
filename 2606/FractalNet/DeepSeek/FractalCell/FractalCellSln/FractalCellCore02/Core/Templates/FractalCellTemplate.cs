// Core/Templates/ractalCellTemplate.cs

using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Templates;

// FractalCellCore/Core/Templates/FractalCellTemplate.cs (ДОБАВЛЯЕМ)

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

    // 🔥 НОВОЕ: Список поведений
    protected readonly List<IBehavior> _behaviors = new();
    private readonly object _behaviorsLock = new();

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

    // 🔥 НОВЫЙ МЕТОД: Добавление поведения
    public void AddBehavior(IBehavior behavior)
    {
        lock (_behaviorsLock)
        {
            _behaviors.Add(behavior);
            Logger.LogInformation("Behavior {BehaviorId} added to cell {CellId}",
                behavior.BehaviorId, CellId);
        }
    }

    // 🔥 НОВЫЙ МЕТОД: Получение всех поведений
    public IReadOnlyList<IBehavior> GetBehaviors()
    {
        lock (_behaviorsLock)
        {
            return _behaviors.ToList().AsReadOnly();
        }
    }

    // 🔥 НОВЫЙ МЕТОД: Роутер событий
    private async Task RouteEventToBehaviorsAsync(IApplicationEvent @event)
    {
        IBehavior[] behaviorsCopy;
        lock (_behaviorsLock)
        {
            // Сортируем по приоритету
            behaviorsCopy = _behaviors
                .OrderBy(b => b.Priority)
                .ToArray();
        }

        if (behaviorsCopy.Length == 0)
        {
            Logger.LogDebug("No behaviors registered for cell {CellId}", CellId);
            return;
        }

        var handlingTasks = new List<Task>();

        foreach (var behavior in behaviorsCopy)
        {
            try
            {
                if (await behavior.CanHandleAsync(@event))
                {
                    Logger.LogDebug("Behavior {BehaviorId} can handle event {EventId}",
                        behavior.BehaviorId, @event.EventId);
                    handlingTasks.Add(behavior.HandleAsync(@event));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking behavior {BehaviorId} for event {EventId}",
                    behavior.BehaviorId, @event.EventId);
            }
        }

        if (handlingTasks.Count > 0)
        {
            await Task.WhenAll(handlingTasks);
            Logger.LogDebug("Event {EventId} processed by {Count} behaviors",
                @event.EventId, handlingTasks.Count);
        }
        else
        {
            Logger.LogDebug("No behavior could handle event {EventId}", @event.EventId);
        }
    }

    // 🔥 МОДИФИЦИРУЕМ ExecuteAsync - теперь события идут через роутер
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

        try
        {
            await foreach (var @event in ExternalBusField.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // 🔥 Сначала отправляем события в поведения
                    await RouteEventToBehaviorsAsync(@event);

                    // 🔥 Потом публикуем во внутреннюю шину (для обратной совместимости)
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

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

        foreach (var bs in CreateBackgroundServices())
        {
            _backgroundServices.Add(bs);
            await bs.StartAsync(cancellationToken);
        }

        await InternalBusField.StartAsync(cancellationToken);

        _ = ExecuteAsync(cancellationToken);

        Logger.LogInformation("FractalCell {CellId} started successfully with {BehaviorCount} behaviors",
            Configuration.CellId, _behaviors.Count);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

        await InternalBusField.StopAsync();

        foreach (var bs in _backgroundServices)
        {
            await bs.StopAsync(cancellationToken);
        }

        // 🔥 Открепляем поведения
        foreach (var behavior in _behaviors)
        {
            try
            {
                await behavior.DetachAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error detaching behavior {BehaviorId}", behavior.BehaviorId);
            }
        }
        _behaviors.Clear();

        Logger.LogInformation("FractalCell {CellId} stopped", Configuration.CellId);
    }
}

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
