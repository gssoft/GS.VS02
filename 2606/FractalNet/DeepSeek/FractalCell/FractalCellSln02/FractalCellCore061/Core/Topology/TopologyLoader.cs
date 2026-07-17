// Core/Topology/TopologyLoader.cs
using System.Text.Json;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Topology;

namespace FractalCellCore.Core.Topology;

/// <summary>
/// Загрузчик топологии из JSON. Для создания узлов использует набор INodeFactory.
/// </summary>
public class TopologyLoader
{
    private readonly IEnumerable<INodeFactory> _factories;
    private readonly IServiceProvider _sp;

    public TopologyLoader(IEnumerable<INodeFactory> factories, IServiceProvider sp)
    {
        _factories = factories;
        _sp = sp;
    }

    public async Task<INode> LoadFromJsonAsync(string json, CancellationToken ct = default)
    {
        var descriptor = JsonSerializer.Deserialize<NodeDescriptor>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Invalid topology JSON");

        return await BuildNodeAsync(descriptor, ct);
    }

    private async Task<INode> BuildNodeAsync(NodeDescriptor desc, CancellationToken ct)
    {
        var factory = _factories.FirstOrDefault(f => string.Equals(f.TypeKey, desc.Type, StringComparison.OrdinalIgnoreCase));
        if (factory == null)
            throw new InvalidOperationException($"No factory registered for type '{desc.Type}'");

        var node = await factory.CreateAsync(desc, ct);

        if (node is ICompositeNode comp && desc.Children != null)
        {
            foreach (var childDesc in desc.Children)
            {
                var child = await BuildNodeAsync(childDesc, ct);
                await comp.AddChildAsync(child, ct);
            }
        }

        return node;
    }
}

