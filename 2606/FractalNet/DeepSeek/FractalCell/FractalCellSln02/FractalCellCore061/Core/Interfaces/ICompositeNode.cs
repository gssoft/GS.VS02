// Core/Interfaces/ICompositeNode.cs
using System.Collections.Generic;
using System.Threading;

namespace FractalCellCore.Core.Interfaces;

/// <summary>
/// Узел, содержащий дочерние узлы (оркестратор / composite).
/// </summary>
public interface ICompositeNode : INode
{
    IReadOnlyList<INode> Children { get; }

    Task AddChildAsync(INode child, CancellationToken ct = default);
    Task RemoveChildAsync(string nodeId, CancellationToken ct = default);
}

