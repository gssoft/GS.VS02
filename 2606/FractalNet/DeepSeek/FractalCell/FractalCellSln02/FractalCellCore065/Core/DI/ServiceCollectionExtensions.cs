// Core/DI/ServiceCollectionExtensions.cs
using FractalCellCore.Core.Factories;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Topology;
using FractalCellCore.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace FractalCellCore.Core.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFractalNodeInfrastructure(this IServiceCollection services)
    {
        // Node registry
        services.AddSingleton<INodeRegistry, InMemoryNodeRegistry>();

        // Factories for topology loader
        services.AddSingleton<INodeFactory, CompositeOrchestratorFactory>();
        services.AddSingleton<INodeFactory, FractalCellNodeFactory>();

        // Topology loader
        services.AddSingleton<TopologyLoader>();

        return services;
    }
}

