namespace DS.BackgroundServices.Core04;

/// <summary>
/// Юстировочные настройки, оборачивающие выполнение задачи.
/// Позволяют добавлять повторные попытки, таймауты, метрики и т.п.
/// </summary>
public interface IBackgroundTuning
{
    /// <summary>
    /// Выполняет задачу с учётом всех юстировок.
    /// </summary>
    /// <param name="task">Делегат, представляющий полезную работу.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task ExecuteWithTuningAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken);
}
