using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DS.BackgroundServices.Core;
// namespace DS.BackgroundServices.Core;

// namespace DS.BackgroundServices.Workers;

/// <summary>
/// Надёжная база для любого фонового сервиса.
/// Предоставляет правильную асинхронную остановку, логирование с именем конечного класса
/// и хуки жизненного цикла.
/// </summary>
public abstract class BackgroundServiceBase : BackgroundService
{
    private readonly ILogger _logger;

    protected BackgroundServiceBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Реализует основной цикл или логику работы. Вызывается после <see cref="OnStartingAsync"/>.
    /// </summary>
    protected abstract Task ExecuteCoreAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Вызывается один раз перед входом в <see cref="ExecuteCoreAsync"/>.
    /// Идеально для инициализации, открытия соединений и т.п.
    /// </summary>
    protected virtual Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Вызывается после завершения основного цикла, даже при ошибке.
    /// Гарантирует корректное освобождение ресурсов.
    /// </summary>
    protected virtual Task OnStoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Сервис {ServiceName} запускается", GetType().Name);

            await OnStartingAsync(stoppingToken).ConfigureAwait(false);

            await ExecuteCoreAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Сервис {ServiceName} остановлен по запросу хоста", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критический сбой в сервисе {ServiceName}", GetType().Name);
            throw; // Даём хосту возможность отреагировать (например, перезапустить)
        }
        finally
        {
            // Асинхронная очистка – теперь без блокировок и deadlock'ов
            try
            {
                await OnStoppedAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Ожидаемо при штатной остановке
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при финальной очистке сервиса {ServiceName}", GetType().Name);
            }

            _logger.LogInformation("Сервис {ServiceName} завершил работу", GetType().Name);
        }
    }
}