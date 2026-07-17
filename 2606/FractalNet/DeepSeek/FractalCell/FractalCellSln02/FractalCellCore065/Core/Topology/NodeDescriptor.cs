// Core/Topology/NodeDescriptor.cs
using System.Collections.Generic;

namespace FractalCellCore.Core.Topology;

/// <summary>
/// Промежуточная модель для десериализации JSON топологии.
/// </summary>
public class NodeDescriptor
{
    public string NodeId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object>? Settings { get; set; }
    public List<string>? Behaviors { get; set; }
    public List<NodeDescriptor>? Children { get; set; }
}
