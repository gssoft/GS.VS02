// Bus/IMessageBus.cs
interface IMessageBus
{
    ValueTask PublishAsync(IMessage message, CancellationToken ct = default);
    ValueTask SendAsync(ICommand command, CancellationToken ct = default);
}
