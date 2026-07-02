// using DS.BackgroundServices.Core.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

// namespace DS.BackgroundServices.Core.Workers;
namespace DS.BackgroundServices.Core02;

// Периодическая обработка каждые 10 секунд
public class CustomDataProcessor : PeriodicBackgroundService
{
    private readonly ILogger _logger;
    public CustomDataProcessor(ILogger<CustomDataProcessor> logger)
        : base(logger, TimeSpan.FromSeconds(10))
    {
        _logger = logger;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
       _logger.LogInformation("Обработка данных...");
        // бизнес-логика
        await Task.Delay(500, stoppingToken); // имитация работы
    }
}