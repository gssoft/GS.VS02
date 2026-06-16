// Application/Interfaces/IEventBus.cs

using Application.Interfaces;

public interface IEventBus
{
    ValueTask PublishAsync(IEvent @event, CancellationToken ct = default);
}
