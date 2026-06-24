using FractalCell.Core.Interfaces;
using System.Threading.Channels;

namespace FractalCell.Core.Interfaces;

public interface IFractalEventHub
{
    Task RegisterChannelAsync(string cellId, Channel<IApplicationEvent> incomingChannel);
    Task RegisterConsumerAsync(string cellId, Func<IApplicationEvent, Task> consumer);
    Task UnregisterCellAsync(string cellId);
    Task PublishAsync(string targetCellId, IApplicationEvent @event);
    Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null);
    IReadOnlyCollection<string> GetActiveCells();
}