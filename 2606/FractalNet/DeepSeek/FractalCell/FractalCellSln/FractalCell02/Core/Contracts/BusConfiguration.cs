// Core/Contracts/BusConfiguration.cs

public record BusConfiguration
{
    public int Capacity { get; init; } = 1000;
    public int MaxParallelism { get; init; } = 4;
    public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool EnablePersistence { get; init; } = false;
}
