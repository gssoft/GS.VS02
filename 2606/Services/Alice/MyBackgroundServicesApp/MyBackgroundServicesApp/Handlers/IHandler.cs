// Handlers/IHandler.cs

interface IHandler<in TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken ct);
}

