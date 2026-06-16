// Worker.cs

using System.Text.Json; // Для красивого логирования объектов .NET 6+
using System.Threading.Tasks;
using Application.Commands; // Используем нашу команду.
using Application.Interfaces; // Используем шины.
using Microsoft.Extensions.Hosting;
// ... другие using

public class Worker : BackgroundService // Наследуемся от BackgroundService вместо старого WorkerBase (.NET 6+)
{
    private readonly ICommandBus _commandBus; // Инжектим шину команд.

    public Worker(ICommandBus commandBus) // Можно инжектить и IEventBus напрямую, если нужно публиковать события из воркера.
    {
        _commandBus = commandBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int i = 0; // Счетчик для имитации работы.

        while (!stoppingToken.IsCancellationRequested)
        {
            i++;

            // Логика воркера: например, каждую 5-ю итерацию создаем нового пользователя.
            if (i % 5 == 0)
            {
                var newUsername = $"User_{DateTime.Now:HHmmss}_{i}";
                var createUserCmd = new CreateUserCommand { Username = newUsername };

                Console.WriteLine($"[Worker] Sending command: {JsonSerializer.Serialize(createUserCmd)}");
                await _commandBus.SendAsync(createUserCmd, stoppingToken);
            }

            await Task.Delay(1000, stoppingToken); // Пауза в 1 секунду.
            Console.WriteLine($"[Worker] Worker running at: {DateTimeOffset.Now}");

            if (i > 20) i = 0; // Сбросим счетчик после 20 итераций.

            stoppingToken.ThrowIfCancellationRequested();
        }

        Console.WriteLine("[Worker] Background service is stopping.");
    }
}

