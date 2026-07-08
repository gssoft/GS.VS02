// Core/Templates/ExternalBusTemplate.cs

using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.Interfaces;

using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Templates;

public abstract class ExternalBusTemplate : IExternalBus
{
    protected readonly BusSettings Config;
    protected readonly ILogger? _logger;
    public string BusId { get; }

    protected ExternalBusTemplate(string busId, BusSettings config, ILogger? logger = null)
    {
        BusId = busId;
        Config = config;
        _logger = logger;
    }

    public abstract Task ConnectToHubAsync(IFractalEventHub hub, string cellId);
    public abstract Task SendToCellAsync(string targetCellId, IApplicationEvent @event);
    public abstract Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null);
    public abstract IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
