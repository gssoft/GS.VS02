// BusMicro/ExecutionMode.cs
namespace BusMicro;

/// <summary>
/// Режим выполнения обработчиков.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Обработчики выполняются параллельно.
    /// </summary>
    Parallel,

    /// <summary>
    /// Обработчики выполняются последовательно.
    /// </summary>
    Sequential
}

