// Services/MetricsService.cs

using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActionBlockHubDemo.Services
{
    public class MetricsService : BackgroundService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly IActionBlockHub<string, MyDataType> _actionHub;

        // ❗ НОВОЕ: Больше не хардкодим ключи, берем их из конфига
        private readonly List<string> _keys;

        public MetricsService(
            ILogger<MetricsService> logger,
            IActionBlockHub<string, MyDataType> actionHub,
            IOptions<HubOptions> hubOptions) // ❗ Внедряем настройки
        {
            _logger = logger;
            _actionHub = actionHub;

            // ❗ НОВОЕ: Запоминаем ключи из конфигурации
            _keys = hubOptions.Value.ActionBlock.Keys;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(2000, stoppingToken);
                _logger.LogInformation("MetricsService запущен. Начинаем мониторинг...");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var key in _keys) // Используем ключи из конфига
                        {
                            _logger.LogInformation(
                                "📊 Метрика [{Key}]: Успешно = {Processed}, Ошибок = {Errors}",
                                key, _actionHub.GetProcessedCount(key), _actionHub.GetErrorCount(key));
                        }
                        await Task.Delay(5000, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "MetricsService: Произошла ошибка {Message}", ex.Message);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("MetricsService is shutting down gracefully...");
            }
        }
    }
}
