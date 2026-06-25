using FractalCellCore.Core.Interfaces;


namespace FractalCellCore.Core.Interfaces;

public interface IExternalBus : IDisposable
{
    string BusId { get; }
    Task ConnectToHubAsync(IFractalEventHub hub, string cellId);
    Task SendToCellAsync(string targetCellId, IApplicationEvent @event);
    Task BroadcastAsync(IApplicationEvent @event, Predicate<string>? filter = null);
    IAsyncEnumerable<IApplicationEvent> ReadAllAsync(CancellationToken ct);
}
