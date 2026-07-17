// Core/Adapters/FractalCellNodeAdapter.cs
using FractalCellCore.Core.Interfaces;

namespace FractalCellCore.Adapters;

/// <summary>
/// Адаптер, позволяющий использовать IFractalCell как INode (листовой узел).
/// RouteAsync ожидает, что RouteRequest.Payload может быть FractalEvent,
/// тогда адаптер публикует событие во внешнюю шину ячейки.
/// </summary>
public class FractalCellNodeAdapter : INode
{
    private readonly IFractalCell _cell;

    public string NodeId => _cell.CellId;

    public FractalCellNodeAdapter(IFractalCell cell)
    {
        _cell = cell;
    }

    public Task StartAsync(CancellationToken ct = default) => _cell.StartAsync(ct);

    public Task StopAsync(CancellationToken ct = default) => _cell.StopAsync(ct);

    public Task<INode?> FindAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path) || path == NodeId) return Task.FromResult<INode?>(this);
        return Task.FromResult<INode?>(null);
    }

    public async Task<RouteResult> RouteAsync(RouteRequest request, CancellationToken ct = default)
    {
        // Если payload — FractalEvent, отправляем его во внешнюю шину ячейки
        if (request.Payload is FractalCellCore.Model.FractalEvent appEvent)
        {
            try
            {
                // Отправляем событие в ExternalBus целевой ячейки
                await _cell.ExternalBus.SendToCellAsync(appEvent.TargetCellId, appEvent);
                return RouteResult.Ok();
            }
            catch (Exception ex)
            {
                return RouteResult.Error(ex.Message);
            }
        }

        return RouteResult.Error("Unsupported payload type for FractalCellNodeAdapter");
    }
}
