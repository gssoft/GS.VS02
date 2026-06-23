// Core/Configuration/FractalEventHubConfiguration.cs
public record FractalEventHubConfiguration
{
    public int ChannelCapacity { get; init; } = 1000;
    public bool EnablePersistence { get; init; } = false;
    public string? PersistencePath { get; init; }
    public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
