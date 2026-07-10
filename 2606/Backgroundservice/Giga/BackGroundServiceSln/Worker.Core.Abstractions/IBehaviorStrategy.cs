namespace Worker.Core.Abstractions;

public interface IBehaviorStrategy : IDisposable
{
    Task ExecuteAsync(CancellationToken token);
}
