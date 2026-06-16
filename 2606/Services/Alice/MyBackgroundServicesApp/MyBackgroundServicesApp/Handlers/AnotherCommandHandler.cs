// Handlers/AnotherCommandHandler.cs
class AnotherCommandHandler : IHandler<AnotherCommand>
{
    public async Task HandleAsync(AnotherCommand message, CancellationToken ct)
    {
        Console.WriteLine($"AnotherCommandHandler: Processing correlation {message.CorrelationId} at {message.Timestamp}");
        await Task.Delay(500, ct);
    }
}

