namespace FractalCell02.Core.Configuration;

public record FractalCellConfiguration
{
    public string CellId { get; init; } = string.Empty;
    public int BackgroundServiceCount { get; init; } = 2;
    public BusType InternalBusType { get; init; } = BusType.Channels;
    public BusType ExternalBusType { get; init; } = BusType.TplDataflow;

    public BusSettings BusSettings { get; init; } = new();
    public HubSettings HubSettings { get; init; } = new();
}

public record BusSettings
{
    public int Capacity { get; init; } = 1000;
    public int MaxParallelism { get; init; } = 4;
    public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool EnablePersistence { get; init; } = false;
    public string? PersistencePath { get; init; }
}

public record HubSettings
{
    public int ChannelCapacity { get; init; } = 1000;
    public bool EnablePersistence { get; init; } = false;
    public string? PersistencePath { get; init; }
    public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

public enum BusType
{
    Channels,
    TplDataflow
}
