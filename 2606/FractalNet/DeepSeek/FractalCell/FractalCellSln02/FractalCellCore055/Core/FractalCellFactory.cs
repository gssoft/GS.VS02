// Core/FractalCellFactory.fixed.cs

using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Implementations.Channels;
using FractalCellCore.Implementations.TplDataflow;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FractalCellCore.Implementations;

/// <summary>
/// Фабрика для создания фрактальных ячеек (исправленная версия)
/// </summary>
public static class FractalCellFactory
{
    public static async Task<IFractalCell> CreateAsync(
        FractalCellConfiguration config,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory,
        CancellationToken ct = default)
    {
        IInternalBus internalBus = CreateInternalBus(config);
        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

        await externalBus.ConnectToHubAsync(hub, config.CellId);

        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

        await cell.InitializeAsync();

        return cell;
    }

    /// <summary>
    /// Создание ячейки с одним поведением (исправлено: ConfigureAsync вызывается после AttachToAsync)
    /// </summary>
    public static async Task<IFractalCell> CreateWithBehaviorAsync<TBehavior>(
        FractalCellConfiguration config,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory,
        object? behaviorConfig = null,
        CancellationToken ct = default)
        where TBehavior : IBehavior, new()
    {
        var behavior = new TBehavior();

        var cell = await CreateWithBehaviorsAsync(
            config,
            hub,
            loggerFactory,
            new IBehavior[] { behavior },
            ct);

        if (behaviorConfig != null)
            await behavior.ConfigureAsync(behaviorConfig, ct);

        return cell;
    }

    /// <summary>
    /// Создание ячейки с несколькими поведениями (исправлено: Attach → Configure)
    /// </summary>
    public static async Task<IFractalCell> CreateWithBehaviorsAsync(
        FractalCellConfiguration config,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory,
        IEnumerable<IBehavior> behaviors,
        CancellationToken ct = default)
    {
        IInternalBus internalBus = CreateInternalBus(config);
        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

        await externalBus.ConnectToHubAsync(hub, config.CellId);

        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory, behaviors);

        await cell.InitializeAsync();

        // Исправлено: AttachToAsync вызывается до ConfigureAsync
        foreach (var behavior in behaviors)
        {
            await behavior.AttachToAsync(cell, ct);
        }

        foreach (var behavior in behaviors)
        {
            await behavior.ConfigureAsync(null, ct);
        }

        return cell;
    }

    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
    {
        return config.InternalBusType switch
        {
            BusType.Channels => new ChannelInternalBus(
                $"{config.CellId}-internal",
                config.BusSettings),

            BusType.TplDataflow => new TplInternalBus(
                $"{config.CellId}-internal",
                config.BusSettings),

            _ => throw new NotSupportedException(
                $"Bus type {config.InternalBusType} not supported")
        };
    }

    private static IExternalBus CreateExternalBus(FractalCellConfiguration config, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger($"ExternalBus-{config.CellId}");

        return config.ExternalBusType switch
        {
            BusType.Channels => new ChannelExternalBus(
                $"{config.CellId}-external",
                config.BusSettings,
                logger),

            BusType.TplDataflow => new TplExternalBus(
                $"{config.CellId}-external",
                config.BusSettings,
                logger),

            _ => throw new NotSupportedException(
                $"Bus type {config.ExternalBusType} not supported")
        };
    }

    private static IFractalCell CreateCellInstance(
        FractalCellConfiguration config,
        IInternalBus internalBus,
        IExternalBus externalBus,
        ILoggerFactory loggerFactory,
        IEnumerable<IBehavior>? behaviors = null)
    {
        behaviors ??= Enumerable.Empty<IBehavior>();

        return (config.InternalBusType, config.ExternalBusType) switch
        {
            (BusType.Channels, BusType.Channels) =>
                new ChannelFractalCell(
                    config,
                    (ChannelInternalBus)internalBus,
                    (ChannelExternalBus)externalBus,
                    loggerFactory.CreateLogger<ChannelFractalCell>(),
                    behaviors),

            (BusType.TplDataflow, BusType.TplDataflow) =>
                new TplFractalCell(
                    config,
                    (TplInternalBus)internalBus,
                    (TplExternalBus)externalBus,
                    loggerFactory.CreateLogger<TplFractalCell>(),
                    behaviors),

            _ => throw new NotSupportedException(
                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
        };
    }
}

// 26.07.08
// Core/FractalCellFactory.cs

//using FractalCellCore.Core.Configuration;
//using FractalCellCore.Core.Interfaces;
//using FractalCellCore.Implementations.Channels;
//using FractalCellCore.Implementations.TplDataflow;
//using FractalCellCore.Core.Templates;
//using Microsoft.Extensions.Logging;

//namespace FractalCellCore.Implementations;

//public static class FractalCellFactory
//{
//    public static async Task<IFractalCell> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        IInternalBus internalBus = CreateInternalBus(config);
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        await cell.InitializeAsync();

//        return cell;
//    }



//    public static async Task<IFractalCell> CreateWithBehaviorAsync<TBehavior>(
//    FractalCellConfiguration config,
//    IFractalEventHub hub,
//    ILoggerFactory loggerFactory,
//    object? behaviorConfig = null,
//    CancellationToken ct = default)
//    where TBehavior : IBehavior, new()
//    {
//        var behavior = new TBehavior();
//        if (behaviorConfig != null)
//            await behavior.ConfigureAsync(behaviorConfig, ct);

//        // Явно приводим массив к IEnumerable<IBehavior>
//        return await CreateWithBehaviorsAsync(
//            config,
//            hub,
//            loggerFactory,
//            new IBehavior[] { behavior },  // <-- явное приведение
//            ct);
//    }

//    /// <summary>
//    /// Создание ячейки с несколькими поведениями (использует новый конструктор)
//    /// </summary>
//    public static async Task<IFractalCell> CreateWithBehaviorsAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        IEnumerable<IBehavior> behaviors,
//        CancellationToken ct = default)
//    {
//        IInternalBus internalBus = CreateInternalBus(config);
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаём ячейку, передавая поведения в конструктор
//        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory, behaviors);

//        await cell.InitializeAsync();

//        // Поведения уже добавлены в ячейку через конструктор, поэтому AttachToAsync не нужен.
//        // Однако, если вы хотите, чтобы поведение знало о ячейке, можно вызвать AttachToAsync для каждого,
//        // но в нашем шаблоне AttachToAsync будет вызван внутри конструктора? Нет, мы сами вызываем AddBehavior.
//        // Чтобы поведение получило ссылку на ячейку, нужно вызвать AttachToAsync.
//        // Но мы этого не делаем, потому что роутер не требует AttachToAsync для работы, он только вызывает методы CanHandleAsync и HandleAsync.
//        // Если же поведение использует _attachedCell, то нужно вызывать AttachToAsync.
//        // Рекомендуем вызывать AttachToAsync для каждого поведения, чтобы они могли получить доступ к ячейке.
//        // Для этого пройдём по поведениям и вызовем AttachToAsync.
//        // Но учтите, что AttachToAsync может быть вызван только один раз.
//        // В нашем случае, если мы уже вызвали AttachToAsync в фабрике ранее (в старом варианте), то сейчас мы его не вызывали.
//        // Поэтому вызовем AttachToAsync здесь.
//        // Однако, мы передаём поведения в конструктор, но они ещё не привязаны к ячейке.
//        // Поэтому после создания ячейки привяжем их.
//        foreach (var behavior in behaviors)
//        {
//            await behavior.AttachToAsync(cell, ct);
//        }

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
//        // ... без изменений
//        return config.InternalBusType switch
//        {
//            BusType.Channels => new ChannelInternalBus(
//                $"{config.CellId}-internal",
//                config.BusSettings),
//            BusType.TplDataflow => new TplInternalBus(
//                $"{config.CellId}-internal",
//                config.BusSettings),
//            _ => throw new NotSupportedException(
//                $"Bus type {config.InternalBusType} not supported")
//        };
//    }

//    private static IExternalBus CreateExternalBus(FractalCellConfiguration config, ILoggerFactory loggerFactory)
//    {
//        // ... без изменений
//        var logger = loggerFactory.CreateLogger($"ExternalBus-{config.CellId}");

//        return config.ExternalBusType switch
//        {
//            BusType.Channels => new ChannelExternalBus(
//                $"{config.CellId}-external",
//                config.BusSettings,
//                logger),
//            BusType.TplDataflow => new TplExternalBus(
//                $"{config.CellId}-external",
//                config.BusSettings,
//                logger),
//            _ => throw new NotSupportedException(
//                $"Bus type {config.ExternalBusType} not supported")
//        };
//    }

//    // Новая перегрузка CreateCellInstance с поведениями
//    private static IFractalCell CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory,
//        IEnumerable<IBehavior>? behaviors = null)
//    {
//        behaviors ??= Enumerable.Empty<IBehavior>();

//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>(),
//                    behaviors),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>(),
//                    behaviors),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}


