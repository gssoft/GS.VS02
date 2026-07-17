// Core/Interfaces/INodeRegistry.cs
using System.Collections.Generic;
using System.Threading;

namespace FractalCellCore.Core.Interfaces;

/// <summary>
/// Простой реестр узлов (Node Bus). Позволяет быстро находить узел по NodeId.
/// Реализация по умолчанию — in-memory.
/// </summary>
public interface INodeRegistry
{
    Task RegisterAsync(INode node, CancellationToken ct = default);
    Task UnregisterAsync(string nodeId, CancellationToken ct = default);
    Task<INode?> LookupAsync(string nodeId, CancellationToken ct = default);
    Task<IEnumerable<string>> ListNodeIdsAsync(CancellationToken ct = default);
}

