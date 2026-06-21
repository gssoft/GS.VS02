// Interface/IFractalEventHub.cs
using System.Threading.Channels;

public interface IFractalEventHub
{
    Task RegisterCellAsync(string cellId, Channel<IApplicationEvent> incomingChannel,
        FractalEventHubConfiguration? config = null);
    Task UnregisterCellAsync(string cellId);
    Task PublishAsync(string targetCellId, IApplicationEvent @event);
    Task PublishToAllAsync(IApplicationEvent @event, Predicate<string>? filter = null);
}

