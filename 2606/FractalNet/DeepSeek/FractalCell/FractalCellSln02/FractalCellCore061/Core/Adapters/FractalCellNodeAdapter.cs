// Core/Adapters/FractalCellNodeAdapter.cs
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FractalCellCore.Core.Interfaces;

namespace FractalCellCore.Adapters;

/// <summary>
/// Адаптер, позволяющий использовать IFractalCell как INode (листовой узел).
/// RouteAsync принимает payload типа IApplicationEvent и пересылает его во внешнюю шину ячейки.
/// </summary>
public class FractalCellNodeAdapter : INode
{
    private readonly IFractalCell _cell;

    public string NodeId => _cell.CellId;

    public FractalCellNodeAdapter(IFractalCell cell)
    {
        _cell = cell ?? throw new ArgumentNullException(nameof(cell));
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
        if (request == null) return RouteResult.Error("Request is null");

        // Ожидаем общий контракт IApplicationEvent
        if (request.Payload is IApplicationEvent appEvent)
        {
            try
            {
                // Попытка извлечь TargetCellId через reflection, если такое свойство присутствует
                string? targetCellId = null;

                var prop = appEvent.GetType().GetProperty("TargetCellId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var val = prop.GetValue(appEvent);
                    if (val is string s && !string.IsNullOrWhiteSpace(s))
                        targetCellId = s;
                }

                // Если TargetCellId не найден — отправляем в текущую ячейку
                var destination = !string.IsNullOrWhiteSpace(targetCellId) ? targetCellId : _cell.CellId;

                await _cell.ExternalBus.SendToCellAsync(destination, appEvent);

                return RouteResult.Ok();
            }
            catch (Exception ex)
            {
                return RouteResult.Error($"Failed to route event: {ex.Message}");
            }
        }

        return RouteResult.Error("Unsupported payload type for FractalCellNodeAdapter; expected IApplicationEvent");
    }
}


