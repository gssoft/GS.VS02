// Core/Interfaces/INodeFactory.cs
using System.Threading;

namespace FractalCellCore.Core.Interfaces;

/// <summary>
/// Фабрика для создания INode по дескриптору (используется TopologyLoader).
/// </summary>
public interface INodeFactory
{
    /// <summary>Ключ (имя типа) для сопоставления с JSON "Type".</summary>
    string TypeKey { get; }

    /// <summary>Создать узел по дескриптору.</summary>
    Task<INode> CreateAsync(Core.Topology.NodeDescriptor descriptor, CancellationToken ct = default);
}

