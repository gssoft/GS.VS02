using FractalCell.Core.Configuration;
using FractalCell.Core.Interfaces;
using FractalCell.Core.Templates;
using FractalCell.Implementations.Channels;
using FractalCell.Implementations.TplDataflow;
using Microsoft.Extensions.Logging;

namespace FractalCell.Core;

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

            _ => throw new NotSupportedException(
                $"Combination {config.InternalBusType}/{config.ExternalBusType} not supported")
        };
    }
}
