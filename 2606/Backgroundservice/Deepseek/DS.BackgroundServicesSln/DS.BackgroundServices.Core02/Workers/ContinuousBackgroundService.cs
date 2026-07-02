using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

// namespace DS.BackgroundServices.Core.Workers;
namespace DS.BackgroundServices.Core02;

/// <summary>
/// Сервис, выполняющий работу в бесконечном цикле без искусственных пауз.
/// Используйте, когда нужно постоянно слушать очередь, поток или канал.
/// </summary>
public abstract class ContinuousBackgroundService : BackgroundServiceBase
{
    private readonly ILogger _logger;
    protected ContinuousBackgroundService(ILogger logger)
        : base(logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Один проход непрерывной обработки.
    /// Если метод блокируется внутри асинхронного ожидания – это допустимо.
    /// </summary>
    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в непрерывном цикле сервиса {ServiceName}", GetType().Name);
                // Небольшая защита от «шторма» ошибок
                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
