using MassTransit;

public class MyBusinessWorker : IConsumer<StartWorkingSignal>, IConsumer<StopWorkingSignal>
{
    private bool _isRunning = false;

    public async Task Consume(ConsumeContext<StartWorkingSignal> context)
    {
        Console.WriteLine($"[{DateTime.Now}] Получен сигнал к старту!");
        _isRunning = true;
        // Здесь логика запуска
        while (_isRunning)
        {
            // Делаем работу...
            await Task.Delay(1000);
        }
    }

    public Task Consume(ConsumeContext<StopWorkingSignal> context)
    {
        Console.WriteLine($"[{DateTime.Now}] Получен сигнал к остановке!");
        _isRunning = false; // Это остановит цикл в методе выше
        // Здесь логика финальной очистки
        return Task.CompletedTask;
    }
}
