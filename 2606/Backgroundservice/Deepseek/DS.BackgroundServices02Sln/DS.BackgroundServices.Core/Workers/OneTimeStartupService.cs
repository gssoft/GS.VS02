using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DS.BackgroundServices.Core.Workers;

/// <summary>
/// Сервис, который выполняет задачу один раз при старте приложения и завершается.
/// Хост не будет ждать его повторного запуска.
/// </summary>
public abstract class OneTimeStartupService : BackgroundServiceBase
{
    private readonly ILogger _logger;
    protected OneTimeStartupService(ILogger logger)
        : base(logger)
    {
        _logger = logger;
    }

    protected abstract Task ExecuteOnceAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        await ExecuteOnceAsync(stoppingToken).ConfigureAwait(false);
        // После выполнения метод возвращается – сервис считается завершённым.
    }
}
