namespace DS.BackgroundServices.Core04;

public class TimeoutTuning : IBackgroundTuning
{
    private readonly TimeSpan _timeout;

    public TimeoutTuning(TimeSpan timeout) => _timeout = timeout;

    public async Task ExecuteWithTuningAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        try
        {
            await task(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Истёк таймаут, а не глобальная остановка
            throw new TimeoutException($"Операция не завершилась за {_timeout}");
        }
    }
}
