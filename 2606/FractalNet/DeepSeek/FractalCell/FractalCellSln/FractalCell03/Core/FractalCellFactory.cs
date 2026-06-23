// Core/FractalCellFactory.cs

using System.Runtime.InteropServices.JavaScript;

using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

public static class FractalCellFactory
{
    public static async Task<FractalCellTemplate<IInternalBus, IExternalBus>> CreateAsync(
        FractalCellConfiguration config,
        IFractalEventHub hub,
        ILoggerFactory loggerFactory,
        CancellationToken ct = default)
    {
        // Создаем внутреннюю шину
        var internalBus = CreateInternalBus(config);

        // Создаем внешнюю шину
        var externalBus = CreateExternalBus(config);

        // Подключаем к хабу
        await externalBus.ConnectToHubAsync(hub, config.CellId);

        // Создаем ячейку
        var cell = CreateCellInstance(config, internalBus, externalBus, loggerFactory);

        // Настраиваем обработчики
        cell.ConfigureHandlers();

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

    private static IExternalBus CreateExternalBus(FractalCellConfiguration config)
    {
        return config.ExternalBusType switch
        {
            BusType.Channels => new ChannelExternalBus(
                $"{config.CellId}-external",
                config.BusSettings),
            BusType.TplDataflow => new TplExternalBus(
                $"{config.CellId}-external",
                config.BusSettings),
            _ => throw new NotSupportedException(
                $"Bus type {config.ExternalBusType} not supported")
        };
    }

    private static FractalCellTemplate<IInternalBus, IExternalBus> CreateCellInstance(
        FractalCellConfiguration config,
        IInternalBus internalBus,
        IExternalBus externalBus,
        ILoggerFactory loggerFactory)
    {
        return (config.InternalBusType, config.ExternalBusType) switch
        {
            (BusType.Channels, BusType.Channels) =>
                new ChannelFractalCell(config,
                    (ChannelInternalBus)internalBus,
                    (ChannelExternalBus)externalBus,
                    loggerFactory.CreateLogger<ChannelFractalCell>()),

            (BusType.TplDataflow, BusType.TplDataflow) =>
                new TplFractalCell(config,
                    (TplInternalBus)internalBus,
                    (TplExternalBus)externalBus,
                    loggerFactory.CreateLogger<TplFractalCell>()),

            // Смешанные варианты тоже поддерживаются
            (BusType.Channels, BusType.TplDataflow) =>
                new ChannelFractalCell(config,
                    (ChannelInternalBus)internalBus,
                    (ChannelExternalBus)externalBus,
                    loggerFactory.CreateLogger<ChannelFractalCell>()),

            (BusType.TplDataflow, BusType.Channels) =>
                new TplFractalCell(config,
                    (TplInternalBus)internalBus,
                    (TplExternalBus)externalBus,
                    loggerFactory.CreateLogger<TplFractalCell>()),

            _ => throw new NotSupportedException(
                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
        };
    }
}
