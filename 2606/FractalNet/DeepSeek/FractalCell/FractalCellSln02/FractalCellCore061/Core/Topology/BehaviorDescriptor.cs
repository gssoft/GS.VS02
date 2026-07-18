namespace FractalCellCore.Core.Topology;

using System.Text.Json;

public record BehaviorDescriptor
{
    public string? Name { get; init; }
    public string? Type { get; init; } // "Namespace.TypeName, AssemblyName"
    public JsonElement? Config { get; init; }
}

