// Core/Contracts/CellConfiguration.cs
public record CellConfiguration
{
    public string CellId { get; init; } = string.Empty;
    public CellAddress? Address { get; init; }
    public int BackgroundServiceCount { get; init; } = 2;
    public BusType InternalBusType { get; init; } = BusType.Channels;
    public BusType ExternalBusType { get; init; } = BusType.TplDataflow;
    public int ChannelCapacity { get; init; } = 1000;
    public int MaxParallelism { get; init; } = 4;
}

public enum BusType
{
    Channels,
    TplDataflow
}
