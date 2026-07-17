// Core/Implementations/InMemoryNodeRegistry.cs
using System.Collections.Concurrent;
using FractalCellCore.Core.Interfaces;

namespace FractalCellCore.Implementations;

/// <summary>
/// Простая in-memory реализация реестра узлов.
/// </summary>
public class InMemoryNodeRegistry : INodeRegistry
{
    private readonly ConcurrentDictionary<string, INode> _nodes = new();

    public Task RegisterAsync(INode node, CancellationToken ct = default)
    {
        _nodes[node.NodeId] = node;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string nodeId, CancellationToken ct = default)
    {
        _nodes.TryRemove(nodeId, out _);
        return Task.CompletedTask;
    }

    public Task<INode?> LookupAsync(string nodeId, CancellationToken ct = default)
    {
        _nodes.TryGetValue(nodeId, out var node);
        return Task.FromResult(node);
    }

    public Task<IEnumerable<string>> ListNodeIdsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<string>>(_nodes.Keys.ToList());
    }
}

