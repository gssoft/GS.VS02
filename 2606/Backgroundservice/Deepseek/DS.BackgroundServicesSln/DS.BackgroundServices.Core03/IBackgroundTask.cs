namespace DS.BackgroundServices.Core03;

/// <summary>Полезная работа, выполняемая фоновым сервисом.</summary>
public interface IBackgroundTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
