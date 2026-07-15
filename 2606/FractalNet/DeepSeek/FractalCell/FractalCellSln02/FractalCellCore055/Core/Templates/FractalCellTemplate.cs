// 26.07.12
// Core/Templates/FractalCellTemplate.fixed.cs

using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FractalCellCore.Core.Templates;

/// <summary>
/// Базовый шаблон ячейки фрактальной системы (исправленная версия)
/// </summary>
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

    // Список поведений
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

    // === Управление поведениями ===

    public void AddBehavior(IBehavior behavior)
    {
        lock (_behaviorsLock)
        {
            _behaviors.Add(behavior);
            Logger.LogInformation("Behavior {BehaviorId} added to cell {CellId}",
                behavior.BehaviorId, CellId);
        }
    }

    public IReadOnlyList<IBehavior> GetBehaviors()
    {
        lock (_behaviorsLock)
        {
            return _behaviors.ToList().AsReadOnly();
        }
    }

    // === Роутер событий (исправленный: последовательная обработка) ===

    private async Task RouteEventToBehaviorsAsync(IApplicationEvent @event)
    {
        IBehavior[] behaviorsCopy;

        lock (_behaviorsLock)
        {
            behaviorsCopy = _behaviors
                .OrderBy(b => b.Priority)
                .ToArray();
        }

        if (behaviorsCopy.Length == 0)
        {
            Logger.LogDebug("No behaviors registered for cell {CellId}", CellId);
            return;
        }

        foreach (var behavior in behaviorsCopy)
        {
            try
            {
                if (await behavior.CanHandleAsync(@event))
                {
                    Logger.LogDebug("Behavior {BehaviorId} handles event {EventId}",
                        behavior.BehaviorId, @event.EventId);

                    await behavior.HandleAsync(@event);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Error in behavior {BehaviorId} while processing event {EventId}",
                    behavior.BehaviorId, @event.EventId);
            }
        }
    }

    // === Уведомления о жизненном цикле ===

    private async Task NotifyLifecycleAsync(
        Func<ILifecycleBehavior, CancellationToken, Task> action,
        string phase,
        CancellationToken ct)
    {
        IBehavior[] behaviorsCopy;

        lock (_behaviorsLock)
        {
            behaviorsCopy = _behaviors.ToArray();
        }

        var tasks = new List<Task>();

        foreach (var behavior in behaviorsCopy)
        {
            if (behavior is ILifecycleBehavior lifecycleBehavior)
            {
                tasks.Add(action(lifecycleBehavior, ct));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
            Logger.LogDebug("Lifecycle {Phase} completed for {Count} behaviors in cell {CellId}",
                phase, tasks.Count, CellId);
        }
    }

    // === StartAsync ===

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

        await NotifyLifecycleAsync(
            (b, ct) => b.OnCellStartingAsync(ct),
            nameof(ILifecycleBehavior.OnCellStartingAsync),
            cancellationToken);

        foreach (var bs in CreateBackgroundServices())
        {
            _backgroundServices.Add(bs);
            await bs.StartAsync(cancellationToken);
        }

        await InternalBusField.StartAsync(cancellationToken);

        _ = ExecuteAsync(cancellationToken);

        await NotifyLifecycleAsync(
            (b, ct) => b.OnCellStartedAsync(ct),
            nameof(ILifecycleBehavior.OnCellStartedAsync),
            cancellationToken);

        Logger.LogInformation("FractalCell {CellId} started successfully with {BehaviorCount} behaviors",
            Configuration.CellId, _behaviors.Count);
    }

    // === StopAsync ===

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

        await NotifyLifecycleAsync(
            (b, ct) => b.OnCellStoppingAsync(ct),
            nameof(ILifecycleBehavior.OnCellStoppingAsync),
            cancellationToken);

        await InternalBusField.StopAsync();

        foreach (var bs in _backgroundServices)
        {
            await bs.StopAsync(cancellationToken);
        }

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

        await NotifyLifecycleAsync(
            (b, ct) => b.OnCellStoppedAsync(ct),
            nameof(ILifecycleBehavior.OnCellStoppedAsync),
            cancellationToken);

        Logger.LogInformation("FractalCell {CellId} stopped", Configuration.CellId);
    }

    // === Основной цикл обработки событий (исправленный) ===

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

        await foreach (var evt in ExternalBusField.ReadAllAsync(stoppingToken))
        {
            await RouteEventToBehaviorsAsync(evt);
        }

        Logger.LogInformation("FractalCell {CellId} execute loop finished", Configuration.CellId);
    }
}

// 26.07.08
// Core/Templates/FractalCellTemplate.cs

//using FractalCellCore.Core.Configuration;
//using FractalCellCore.Core.Interfaces;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace FractalCellCore.Core.Templates;

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

//    // Список поведений
//    protected readonly List<IBehavior> _behaviors = new();
//    private readonly object _behaviorsLock = new();

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

//    // === Управление поведениями ===
//    public void AddBehavior(IBehavior behavior)
//    {
//        lock (_behaviorsLock)
//        {
//            _behaviors.Add(behavior);
//            Logger.LogInformation("Behavior {BehaviorId} added to cell {CellId}",
//                behavior.BehaviorId, CellId);
//        }
//    }

//    public IReadOnlyList<IBehavior> GetBehaviors()
//    {
//        lock (_behaviorsLock)
//        {
//            return _behaviors.ToList().AsReadOnly();
//        }
//    }

//    // === Роутер событий ===
//    private async Task RouteEventToBehaviorsAsync(IApplicationEvent @event)
//    {
//        IBehavior[] behaviorsCopy;
//        lock (_behaviorsLock)
//        {
//            behaviorsCopy = _behaviors
//                .OrderBy(b => b.Priority)
//                .ToArray();
//        }

//        if (behaviorsCopy.Length == 0)
//        {
//            Logger.LogDebug("No behaviors registered for cell {CellId}", CellId);
//            return;
//        }

//        var handlingTasks = new List<Task>();

//        foreach (var behavior in behaviorsCopy)
//        {
//            try
//            {
//                if (await behavior.CanHandleAsync(@event))
//                {
//                    Logger.LogDebug("Behavior {BehaviorId} can handle event {EventId}",
//                        behavior.BehaviorId, @event.EventId);
//                    handlingTasks.Add(behavior.HandleAsync(@event));
//                }
//            }
//            catch (Exception ex)
//            {
//                Logger.LogError(ex, "Error checking behavior {BehaviorId} for event {EventId}",
//                    behavior.BehaviorId, @event.EventId);
//            }
//        }

//        if (handlingTasks.Count > 0)
//        {
//            await Task.WhenAll(handlingTasks);
//            Logger.LogDebug("Event {EventId} processed by {Count} behaviors",
//                @event.EventId, handlingTasks.Count);
//        }
//        else
//        {
//            Logger.LogDebug("No behavior could handle event {EventId}", @event.EventId);
//        }
//    }

//    // === НОВОЕ: Уведомления о жизненном цикле ===
//    private async Task NotifyLifecycleAsync(
//        Func<ILifecycleBehavior, CancellationToken, Task> action,
//        string phase,
//        CancellationToken ct)
//    {
//        IBehavior[] behaviorsCopy;
//        lock (_behaviorsLock)
//        {
//            behaviorsCopy = _behaviors.ToArray();
//        }

//        if (behaviorsCopy.Length == 0)
//            return;

//        var tasks = new List<Task>();

//        foreach (var behavior in behaviorsCopy)
//        {
//            if (behavior is ILifecycleBehavior lifecycleBehavior)
//            {
//                tasks.Add(action(lifecycleBehavior, ct));
//            }
//        }

//        if (tasks.Count > 0)
//        {
//            await Task.WhenAll(tasks);
//            Logger.LogDebug("Lifecycle {Phase} completed for {Count} behaviors in cell {CellId}",
//                phase, tasks.Count, CellId);
//        }
//    }

//    // === Переопределённые методы StartAsync и StopAsync ===
//    public override async Task StartAsync(CancellationToken cancellationToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} starting", Configuration.CellId);

//        // 1. Уведомляем поведения о начале запуска
//        await NotifyLifecycleAsync(
//            (b, ct) => b.OnCellStartingAsync(ct),
//            nameof(ILifecycleBehavior.OnCellStartingAsync),
//            cancellationToken);

//        // 2. Запускаем фоновые сервисы
//        foreach (var bs in CreateBackgroundServices())
//        {
//            _backgroundServices.Add(bs);
//            await bs.StartAsync(cancellationToken);
//        }

//        await InternalBusField.StartAsync(cancellationToken);

//        // 3. Запускаем основной цикл обработки событий
//        _ = ExecuteAsync(cancellationToken);

//        // 4. Уведомляем поведения о завершении запуска
//        await NotifyLifecycleAsync(
//            (b, ct) => b.OnCellStartedAsync(ct),
//            nameof(ILifecycleBehavior.OnCellStartedAsync),
//            cancellationToken);

//        Logger.LogInformation("FractalCell {CellId} started successfully with {BehaviorCount} behaviors",
//            Configuration.CellId, _behaviors.Count);
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} stopping", Configuration.CellId);

//        // 1. Уведомляем поведения о начале остановки
//        await NotifyLifecycleAsync(
//            (b, ct) => b.OnCellStoppingAsync(ct),
//            nameof(ILifecycleBehavior.OnCellStoppingAsync),
//            cancellationToken);

//        // 2. Останавливаем внутреннюю шину и фоновые сервисы
//        await InternalBusField.StopAsync();

//        foreach (var bs in _backgroundServices)
//        {
//            await bs.StopAsync(cancellationToken);
//        }

//        // 3. Открепляем поведения
//        foreach (var behavior in _behaviors)
//        {
//            try
//            {
//                await behavior.DetachAsync(cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                Logger.LogError(ex, "Error detaching behavior {BehaviorId}", behavior.BehaviorId);
//            }
//        }
//        _behaviors.Clear();

//        // 4. Уведомляем поведения о завершении остановки
//        await NotifyLifecycleAsync(
//            (b, ct) => b.OnCellStoppedAsync(ct),
//            nameof(ILifecycleBehavior.OnCellStoppedAsync),
//            cancellationToken);

//        Logger.LogInformation("FractalCell {CellId} stopped", Configuration.CellId);
//    }

//    // === Основной цикл обработки событий ===
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        Logger.LogInformation("FractalCell {CellId} execute loop started", Configuration.CellId);

//        try
//        {
//            await foreach (var @event in ExternalBusField.ReadAllAsync(stoppingToken))
//            {
//                try
//                {
//                    // 1. Отправляем события в поведения через роутер
//                    await RouteEventToBehaviorsAsync(@event);
//                    // 2. Публикуем во внутреннюю шину (для обратной совместимости)
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
//}


