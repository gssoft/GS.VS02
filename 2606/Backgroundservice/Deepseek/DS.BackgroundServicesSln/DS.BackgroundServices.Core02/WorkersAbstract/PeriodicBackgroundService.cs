using System;
using System.Collections.Generic;
using System.Text;

// периодическое выполнение с настраиваемым интервалом

using Microsoft.Extensions.Logging;

// namespace DS.BackgroundServices.Core.Workers;
namespace DS.BackgroundServices.Core02;

/// <summary>
/// Фоновый сервис, повторяющий операцию через заданный интервал.
/// Подходит для синхронизаций, опросов и периодической обработки.
/// </summary>
public abstract class PeriodicBackgroundService : BackgroundServiceBase
{
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;

    protected PeriodicBackgroundService(ILogger logger, TimeSpan interval)
        : base(logger)
    {
        _logger = logger;

        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Интервал должен быть положительным");
        _interval = interval;
    }

    /// <summary>
    /// Одна итерация работы.
    /// </summary>
    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        // Первый запуск – сразу после старта, если не нужно ждать первый интервал,
        // можно раскомментировать задержку.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Нормальная остановка – выходим
                break;
            }
            catch (Exception ex)
            {
                // Логируем ошибку итерации, но не обрушиваем весь сервис
                _logger.LogError(ex, "Ошибка в периодической операции сервиса {ServiceName}", GetType().Name);
            }

            // Пауза между итерациями. Если хост уже остановлен, задержка прервётся сразу.
            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }
    }
}