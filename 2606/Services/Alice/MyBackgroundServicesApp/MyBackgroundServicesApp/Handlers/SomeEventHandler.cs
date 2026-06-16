// Handlers/SomeEventHandler.cs
class SomeEventHandler : IHandler<SomeEvent>
{
    public async Task HandleAsync(SomeEvent message, CancellationToken ct)
    {
        Console.WriteLine($"SomeEventHandler: Received event {message.EventType} at {message.OccurredAt}");
        await Task.Delay(300, ct);
    }
}

