// Services/DataProducerService.cs

using EventBus.Abstractions;
using EventHubWorkerService.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHubWorkerService.Services;

public class DataProducerService : BackgroundService
{
    private readonly IEventBus _bus;
    private readonly ILogger<DataProducerService> _logger;
    private int _counter;

    public DataProducerService(IEventBus bus, ILogger<DataProducerService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🚀 Producer started");

            while (!stoppingToken.IsCancellationRequested)
            {
                var @event = new MyEvent(
                    Id: Guid.NewGuid(),
                    Payload: $"Message #{++_counter}",
                    Timestamp: DateTime.UtcNow
                );

                // Публикация: возвращает управление мгновенно!
                await _bus.PublishAsync(@event, stoppingToken);

                _logger.LogDebug("📤 Published event {Id}", @event.Id);

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                // await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken); // Было 2 секунды
            }
        }
        catch (OperationCanceledException)
        {
            // Эта секция выполнится, когда сервис остановят
            _logger.LogInformation("Фоновая задача была корректно прервана.");
        }
        finally
        {
            // Здесь можно освободить ресурсы, если они есть
            _logger.LogInformation("Сервис QuoteGeneratorService полностью остановлен.");
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("📈 Producer готовится к запуску...");

        // Здесь может быть логика инициализации, например:
        // await InitializeDatabaseConnectionAsync(cancellationToken);

        _logger.LogInformation("🚀 Инициализация завершена. Запуск основного цикла.");

        // Вызываем стандартный старт, который начнет ExecuteAsync
        await base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("⚠️ Получен сигнал об остановке сервиса.");

        // Здесь может быть логика очистки, например:
        // await SaveFinalStateToDatabaseAsync(cancellationToken);
        // CloseFileHandles();

        // _logger.LogInformation("🛑 Сервис успешно остановлен.");

        // Вызываем стандартную остановку
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("🛑 Сервис успешно остановлен.");
    }
}
