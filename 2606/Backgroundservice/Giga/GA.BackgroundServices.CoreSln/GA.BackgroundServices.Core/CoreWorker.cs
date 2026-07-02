using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GA.BackgroundServices.Core;

/// <summary>
/// Базовый класс для сервисов компании с централизованной обработкой ошибок 
/// и гарантированным ожиданием завершения при остановке.
/// </summary>
public abstract class WorkerCore : BackgroundService
{
    protected readonly ILogger<WorkerCore> _logger;

    protected WorkerCore(ILogger<WorkerCore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Основная логика сервиса. Должна быть реализована в наследниках.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Сервис {ServiceName} запускается.", GetType().Name);

            // Ожидаем фактического запуска приложения перед началом работы
            await OnStartingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Защита CPU
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Нормальное завершение при остановке хоста
            _logger.LogInformation("Сервис {ServiceName} получил сигнал отмены.", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка в сервисе {ServiceName}.", GetType().Name);
            throw; // Пробросим исключение вверх, чтобы хост отреагировал согласно настройкам Behavior
        }
        finally
        {
            // Блокируем выполнение до тех пор, пока очистка реально не завершится
            var cleanupTask = OnStoppedAsync(stoppingToken);

            // Ждем завершения. Это гарантирует, что логика дочернего класса отработала полностью.
            try
            {
                cleanupTask.GetAwaiter().GetResult();
            }
            catch (Exception cleanupEx) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(cleanupEx, "Ошибка во время финальной очистки.");
            }

            // Теперь мы точно знаем, что всё очищено
            _logger.LogInformation("Сервис {ServiceName} завершил работу.", GetType().Name);


            //await OnStoppedAsync(stoppingToken);
            //_logger.LogInformation("Сервис {ServiceName} завершил работу.", GetType().Name);
        }
    }

    /// <summary>
    /// Вызывается один раз после старта хоста, но до начала основного цикла.
    /// Аналог AsyncStart.
    /// </summary>
    protected virtual Task OnStartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Основной рабочий метод, который нужно переопределить в дочерних классах.
    /// </summary>
    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

    protected virtual async Task OnStoppedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Пример ожидания завершения внутренних операций или таймаута
            await Task.Delay(1000, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Это нормальное поведение при остановке хоста. 
            // Просто выходим из метода, не логгируя ошибку.
            // _logger.LogDebug("Операция остановки была прервана досрочно по запросу хоста.");
            _logger.LogInformation("OnStoppedAsync(): Операция остановки была прервана досрочно по запросу хоста.");
        }
    }
}
