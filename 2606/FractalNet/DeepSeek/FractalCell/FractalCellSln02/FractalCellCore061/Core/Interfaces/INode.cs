// Core/Interfaces/INode.cs
using System.Threading;

namespace FractalCellCore.Core.Interfaces;

public record RouteRequest(string Path, string CorrelationId, object? Payload = null);

public record RouteResult(bool Success, string Message = "", object? Response = null)
{
    public static RouteResult NotFound(string path) => new(false, $"Node not found: {path}");
    public static RouteResult Ok(object? response = null) => new(true, "OK", response);
    public static RouteResult Error(string message) => new(false, message, null);
};

/// <summary>
/// Базовый узел навигации/маршрутизации в дереве оркестраторов.
/// Может быть листом (ячейка) или композитом (оркестратор).
/// </summary>
public interface INode
{
    string NodeId { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Найти узел по относительному пути (segments separated by '/').
    /// Если path пустой или null — вернуть сам узел.
    /// </summary>
    Task<INode?> FindAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Маршрутизировать запрос к узлу, найденному по request.Path.
    /// </summary>
    Task<RouteResult> RouteAsync(RouteRequest request, CancellationToken ct = default);
}

