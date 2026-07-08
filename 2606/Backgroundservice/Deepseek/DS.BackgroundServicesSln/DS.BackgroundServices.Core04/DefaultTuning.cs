namespace DS.BackgroundServices.Core04;

public class DefaultTuning : IBackgroundTuning
{
    public Task ExecuteWithTuningAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken)
        => task(cancellationToken);
}
