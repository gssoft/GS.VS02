using Worker.Core.Abstractions;

namespace Worker.Core.Infrastructure.Utils;

public static class SafeWaiter
{
    /// <summary>
    /// Ожидает наступления целевого времени, учитывая CancellationToken.
    /// Корректно обрабатывает ситуацию отмены хоста во время сна.
    /// </summary>
    public static async Task WaitUntilAsync(DateTimeOffset targetTime, IClock clock, CancellationToken token)
    {
        var waitTime = targetTime - clock.UtcNow;

        if (waitTime > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(waitTime, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Пробрасываем дальше. Это сигнал GenericJobHost о том, 
                // что приложение закрывается и нужно завершать ExecuteAsync.
                throw;
            }
        }
    }
}
