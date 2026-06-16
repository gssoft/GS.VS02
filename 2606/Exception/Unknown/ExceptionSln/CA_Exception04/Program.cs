// Console.WriteLine("Hello, World!");

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// 1. Создаем билдер хоста
var host = Host.CreateDefaultBuilder()
    // 2. Настраиваем сервисы, включая логгеры
    .ConfigureServices((hostContext, services) =>
    {
        // Регистрируем наш сервис/метод как хостед-сервис
        services.AddHostedService<Worker>();

        // Здесь можно добавить другие зависимости, если они нужны
    })
    // 3. Конфигурируем логирование
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders(); // Очистка провайдеров по умолчанию
        logging.AddConsole();     // Добавляем вывод в консоль (можно заменить на AddFile и т.д.)
        logging.SetMinimumLevel(LogLevel.Information); // Устанавливаем минимальный уровень логов
    })
    .Build();

// Запускаем хост
await host.RunAsync();


// --- Наш рабочий класс ---
internal class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;

    // Логгер внедряется через конструктор (Dependency Injection)
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("StartAsync(): Задержка на 1 секунду начата...");
            await Task.Delay(1000, stoppingToken);
            _logger.LogInformation("StartAsync(): Задержка завершена.");

            // Вместо Console.WriteLine
            _logger.LogWarning("StartAsync(): Это предупреждение!");
            _logger.LogError("StartAsync(): Это ошибка!");
        }
        catch (Exception ex)
        {
            // Логируем исключение вместе со стеком вызовов
            _logger.LogError(ex, "StartAsync(): Произошла непредвиденная ошибка в Worker");
            throw; // Пробрасываем дальше, если нужно
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync(): Приложение останавливается.");
        return Task.CompletedTask;
    }
}
