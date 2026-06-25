// Шаг 4: Обновляем FractalCellFactory
// Замените содержимое Core / FractalCellFactory.cs:

using FractalCell02.Core.Behaviors;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Core.Templates;
using FractalCell02.Implementations.Channels;
using FractalCell02.Implementations.TplDataflow;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Core;

public static class FractalCellFactory
{
    public static async Task<IFractalCell> CreateAsync(
        FractalCellConfiguration config,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory,
        ICellBehavior behavior,
        CancellationToken ct = default)
    {
        IInternalBus internalBus = CreateInternalBus(config);
        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

        await externalBus.ConnectToHubAsync(hub, config.CellId);

        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory, behavior);

        await cell.InitializeAsync();

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
        ICellBehavior behavior)
    {
        return (config.InternalBusType, config.ExternalBusType) switch
        {
            (BusType.Channels, BusType.Channels) =>
                new ChannelFractalCell(config,
                    (ChannelInternalBus)internalBus,
                    (ChannelExternalBus)externalBus,
                    loggerFactory.CreateLogger<ChannelFractalCell>(),
                    behavior),

            (BusType.TplDataflow, BusType.TplDataflow) =>
                new TplFractalCell(config,
                    (TplInternalBus)internalBus,
                    (TplExternalBus)externalBus,
                    loggerFactory.CreateLogger<TplFractalCell>(),
                    behavior),

            _ => throw new NotSupportedException(
                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
        };
    }
}

//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using FractalCell02.Implementations.Channels;
//using FractalCell02.Implementations.TplDataflow;
//using Microsoft.Extensions.Logging;

//// namespace FractalCell02.Core;

//public static class FractalCellFactory
//{
//    public static async Task<IFractalCell> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        // Создаем внутреннюю шину
//        IInternalBus internalBus = CreateInternalBus(config);

//        // Создаем внешнюю шину
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        // Подключаем внешнюю шину к хабу
//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаем ячейку
//        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        // Инициализируем ячейку (настраиваем обработчики)
//        await cell.InitializeAsync();

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
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

//    private static IFractalCell CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory)
//    {
//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}

//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using FractalCell02.Implementations.Channels;
//using FractalCell02.Implementations.TplDataflow;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02.Core;

//public static class FractalCellFactory
//{
//    public static async Task<IFractalCell> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        // Создаем внутреннюю шину
//        IInternalBus internalBus = CreateInternalBus(config);

//        // Создаем внешнюю шину
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        // Подключаем внешнюю шину к хабу
//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаем ячейку
//        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        // Инициализируем ячейку (настраиваем обработчики)
//        await cell.InitializeAsync();

//       // _logger?.LogInformation("Cell {CellId} created and initialized", config.CellId);

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
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

//    private static IFractalCell CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory)
//    {
//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}

//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using FractalCell02.Implementations.Channels;
//using FractalCell02.Implementations.TplDataflow;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02.Core;

//public static class FractalCellFactory
//{
//    public static async Task<IFractalCell> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        // Создаем внутреннюю шину
//        IInternalBus internalBus = CreateInternalBus(config);

//        // Создаем внешнюю шину
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        // Подключаем к хабу
//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаем ячейку
//        IFractalCell cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        // Инициализируем
//        await cell.InitializeAsync();

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
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

//    private static IFractalCell CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory)
//    {
//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            (BusType.Channels, BusType.TplDataflow) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.Channels) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}

//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using FractalCell02.Implementations.Channels;
//using FractalCell02.Implementations.TplDataflow;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02.Core;

//public static class FractalCellFactory
//{
//    public static async Task<FractalCellTemplate<IInternalBus, IExternalBus>> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        // Создаем внутреннюю шину
//        IInternalBus internalBus = CreateInternalBus(config);

//        // Создаем внешнюю шину
//        IExternalBus externalBus = CreateExternalBus(config, loggerFactory);

//        // Подключаем к хабу
//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаем ячейку с приведением к нужному типу
//        var cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        // Настраиваем обработчики через публичный метод
//        await cell.InitializeAsync();

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
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

//    private static FractalCellTemplate<IInternalBus, IExternalBus> CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory)
//    {
//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            // Смешанные варианты
//            (BusType.Channels, BusType.TplDataflow) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.Channels) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}

//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Core.Templates;
//using FractalCell02.Implementations.Channels;
//using FractalCell02.Implementations.TplDataflow;
//using Microsoft.Extensions.Logging;

//namespace FractalCell02.Core;

//public static class FractalCellFactory
//{
//    public static async Task<FractalCellTemplate<IInternalBus, IExternalBus>> CreateAsync(
//        FractalCellConfiguration config,
//        IFractalEventHub hub,
//        ILoggerFactory loggerFactory,
//        CancellationToken ct = default)
//    {
//        // Создаем внутреннюю шину
//        var internalBus = CreateInternalBus(config);

//        // Создаем внешнюю шину
//        var externalBus = CreateExternalBus(config, loggerFactory);

//        // Подключаем к хабу
//        await externalBus.ConnectToHubAsync(hub, config.CellId);

//        // Создаем ячейку
//        var cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

//        // Настраиваем обработчики
//        cell.ConfigureHandlers();

//        return cell;
//    }

//    private static IInternalBus CreateInternalBus(FractalCellConfiguration config)
//    {
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

//    private static FractalCellTemplate<IInternalBus, IExternalBus> CreateCellInstance(
//        FractalCellConfiguration config,
//        IInternalBus internalBus,
//        IExternalBus externalBus,
//        ILoggerFactory loggerFactory)
//    {
//        return (config.InternalBusType, config.ExternalBusType) switch
//        {
//            (BusType.Channels, BusType.Channels) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.TplDataflow) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            //// Смешанные варианты тоже поддерживаются
//            (BusType.Channels, BusType.TplDataflow) =>
//                new ChannelFractalCell(config,
//                    (ChannelInternalBus)internalBus,
//                    (ChannelExternalBus)externalBus,
//                    loggerFactory.CreateLogger<ChannelFractalCell>()),

//            (BusType.TplDataflow, BusType.Channels) =>
//                new TplFractalCell(config,
//                    (TplInternalBus)internalBus,
//                    (TplExternalBus)externalBus,
//                    loggerFactory.CreateLogger<TplFractalCell>()),

//            _ => throw new NotSupportedException(
//                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
//        };
//    }
//}
