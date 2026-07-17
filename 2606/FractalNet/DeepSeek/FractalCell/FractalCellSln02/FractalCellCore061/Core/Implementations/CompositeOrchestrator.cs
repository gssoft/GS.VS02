// Core/Implementations/CompositeOrchestrator.cs
using System.Collections.Concurrent;
using FractalCellCore.Core.Interfaces;

namespace FractalCellCore.Implementations;

/// <summary>
/// Простейшая реализация ICompositeNode — хранит детей в ConcurrentDictionary,
/// поддерживает FindAsync по пути вида "zoneA/processor-1".
/// </summary>
public class CompositeOrchestrator : ICompositeNode
{
    private readonly ConcurrentDictionary<string, INode> _children = new();

    public string NodeId { get; }

    public CompositeOrchestrator(string nodeId) => NodeId = nodeId;

    public IReadOnlyList<INode> Children => _children.Values.ToList();

    public Task AddChildAsync(INode child, CancellationToken ct = default)
    {
        _children[child.NodeId] = child;
        return Task.CompletedTask;
    }

    public Task RemoveChildAsync(string nodeId, CancellationToken ct = default)
    {
        _children.TryRemove(nodeId, out _);
        return Task.CompletedTask;
    }

    public async Task<INode?> FindAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return this;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        INode current = this;

        foreach (var seg in segments)
        {
            if (current is ICompositeNode comp)
            {
                var next = comp.Children.FirstOrDefault(c => c.NodeId == seg);
                if (next == null) return null;
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    public async Task<RouteResult> RouteAsync(RouteRequest request, CancellationToken ct = default)
    {
        var target = await FindAsync(request.Path, ct);
        if (target == null) return RouteResult.NotFound(request.Path);
        return await target.RouteAsync(request, ct);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        foreach (var child in _children.Values)
        {
            await child.StartAsync(ct);
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var child in _children.Values)
        {
            await child.StopAsync(ct);
        }
    }
}

