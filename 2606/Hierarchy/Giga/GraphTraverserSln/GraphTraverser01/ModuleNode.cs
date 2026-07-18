public class ModuleNode
{
    public required string Name { get; set; }
    // Ссылка на конкретный тип наследника BackgroundService
    public required Type ServiceType { get; set; }
    public List<ModuleNode> Children { get; set; } = new();
}
