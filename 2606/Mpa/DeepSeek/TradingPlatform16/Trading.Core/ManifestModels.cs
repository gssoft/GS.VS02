namespace Trading.Core;

public class TradingManifest
{
    public string EventBus { get; set; } = "InMemory";
    public string Storage { get; set; } = "InMemory";
    public List<ProcessorManifest> Processors { get; set; } = new();
}

public class ProcessorManifest
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object>? Config { get; set; }
}
