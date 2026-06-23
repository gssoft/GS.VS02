// Core/Interfaces/IFractalEventHub.cs
using System.Collections.Concurrent;
using System.Threading.Channels;

public interface IFractalEventHub
{
    // Для Channel-based шин
    Task RegisterChannelAsync(string cellId, Channel<IApplicationEvent> incomingChannel);

    // Для TPL-based шин
    Task RegisterConsumerAsync(string cellId, Func<IApplicationEvent, Task> consumer);

    Task UnregisterCellAsync(string cellId);
    Task PublishAsync(string targetCellId, IApplicationEvent @event);
    Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null);
    IReadOnlyCollection<string> GetActiveCells();
}


