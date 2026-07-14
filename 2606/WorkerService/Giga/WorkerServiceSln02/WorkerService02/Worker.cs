using System.Collections.Concurrent;
using WorkerServiceSln02.Contracts;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageQueue _queue;

    public Worker(ILogger<Worker> logger, IMessageQueue queue)
    {
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновый воркер запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var message))
            {
                _logger.LogInformation("Получена задача: {Message}", message);
                await Task.Delay(2000, stoppingToken); // Имитация долгой работы
            }
            else
            {
                // Если задач нет, спим 1 секунду, чтобы не грузить CPU
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

// Для примера используем простую потокобезопасную очередь вместо БД
//public interface IMessageQueue { void Enqueue(string msg); bool TryDequeue(out string? msg); }

//public class InMemoryQueue : ConcurrentQueue<string>, IMessageQueue
//{
//    public void Enqueue(string msg) => base.Enqueue(msg);
//    public bool TryDequeue(out string? msg) => base.TryDequeue(out msg);
//}
