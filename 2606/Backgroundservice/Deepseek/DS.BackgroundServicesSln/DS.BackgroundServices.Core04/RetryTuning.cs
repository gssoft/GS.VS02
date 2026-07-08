using Microsoft.Extensions.Logging;

namespace DS.BackgroundServices.Core04;

public class RetryTuning : IBackgroundTuning
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;
    private readonly ILogger<RetryTuning> _logger;

    public RetryTuning(int maxRetries, TimeSpan delay, ILogger<RetryTuning> logger)
    {
        _maxRetries = maxRetries;
        _delay = delay;
        _logger = logger;
    }

    public async Task ExecuteWithTuningAsync(
        Func<CancellationToken, Task> task,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await task(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Попытка {Attempt} не удалась", attempt);
                if (attempt == _maxRetries)
                    throw; // все попытки исчерпаны
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
