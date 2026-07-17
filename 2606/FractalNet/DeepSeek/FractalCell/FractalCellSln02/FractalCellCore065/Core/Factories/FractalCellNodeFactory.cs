// Core/Factories/FractalCellNodeFactory.cs
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Topology;
using FractalCellCore.Core.Configuration;
using FractalCellCore.Implementations;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Factories;

/// <summary>
/// Фабрика для создания FractalCell и обёртки FractalCellNodeAdapter.
/// Использует FractalCellFactory.CreateAsync/CreateWithBehaviorsAsync.
/// Требует IFractalEventHub и ILoggerFactory в DI.
/// </summary>
public class FractalCellNodeFactory : INodeFactory
{
    public string TypeKey => "FractalCell";

    private readonly IFractalEventHub _hub;
    private readonly ILoggerFactory _loggerFactory;

    public FractalCellNodeFactory(IFractalEventHub hub, ILoggerFactory loggerFactory)
    {
        _hub = hub;
        _loggerFactory = loggerFactory;
    }

    public async Task<INode> CreateAsync(NodeDescriptor descriptor, CancellationToken ct = default)
    {
        // Простейшая логика: читаем настройки CellId и BackgroundServiceCount
        var settings = descriptor.Settings ?? new Dictionary<string, object>();
        var cellId = settings.TryGetValue("CellId", out var cid) ? cid?.ToString() ?? descriptor.NodeId : descriptor.NodeId;
        var workers = settings.TryGetValue("BackgroundServiceCount", out var w) && int.TryParse(w?.ToString(), out var wi) ? wi : 1;

        var config = new FractalCellConfiguration
        {
            CellId = cellId,
            BackgroundServiceCount = workers,
            InternalBusType = BusType.Channels,
            ExternalBusType = BusType.Channels,
            BusSettings = new BusSettings { Capacity = 1000, MaxParallelism = 4 }
        };

        // Создаём ячейку через фабрику
        var cell = await FractalCellFactory.CreateAsync(config, _hub, _loggerFactory, ct);

        // Оборачиваем в адаптер
        var node = new FractalCellCore.Adapters.FractalCellNodeAdapter(cell);
        return node;
    }
}

