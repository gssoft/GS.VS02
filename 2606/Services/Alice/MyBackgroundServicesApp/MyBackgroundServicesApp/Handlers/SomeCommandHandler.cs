// Handlers/SomeCommandHandler.cs
class SomeCommandHandler : IHandler<SomeCommand>
{
    private readonly IMessageBus _bus;

    public SomeCommandHandler(IMessageBus bus)
    {
        _bus = bus;
    }

    public async Task HandleAsync(SomeCommand message, CancellationToken ct)
    {
        Console.WriteLine($"SomeCommandHandler: Processing command {message.Id} with data: {message.Data}");

        // Отправляем событие после обработки команды
        await _bus.PublishAsync(new SomeEvent("CommandProcessed", DateTime.UtcNow), ct);

        await Task.Delay(1000, ct); // Имитация работы
    }
}
