// Core/Factories/CompositeOrchestratorFactory.cs
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Topology;

namespace FractalCellCore.Core.Factories;

/// <summary>
/// Фабрика для CompositeOrchestrator
/// </summary>
public class CompositeOrchestratorFactory : INodeFactory
{
    public string TypeKey => "CompositeOrchestrator";

    public Task<INode> CreateAsync(NodeDescriptor descriptor, CancellationToken ct = default)
    {
        var node = new FractalCellCore.Implementations.CompositeOrchestrator(descriptor.NodeId);
        return Task.FromResult<INode>(node);
    }
}

