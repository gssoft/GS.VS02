// Services/MonitoringService.cs
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerService.Dataflow;
using WorkerService.Models;

namespace WorkerService.Services
{
    public class MonitoringService(QuoteChannel channel, ILogger<MonitoringService> logger) : BackgroundService
    {
        private readonly QuoteChannel _channel = channel;
        private readonly ILogger<MonitoringService> _logger = logger;
        private readonly Dictionary<string, List<decimal>> _priceHistory = new();

        private ActionBlock<Quote>? _monitoringBlock;

        private int _totalQuotes;
        private readonly object _lock = new();

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Блок для мониторинга (подписываемся на Broadcast)
            _monitoringBlock = new ActionBlock<Quote>(
                quote => MonitorQuote(quote),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = DataflowBlockOptions.Unbounded,
                    MaxDegreeOfParallelism = 1, // Последовательная обработка
                    CancellationToken = stoppingToken,
                    NameFormat = "Monitoring"
                });

            // Подписываемся на Broadcast (могут быть многие подписчики)
            _channel.MonitoringBroadcast.LinkTo(
                _monitoringBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            _logger.LogInformation("Monitoring Service запущен. Наблюдаю за потоком котировок...");

            return RunPeriodicReportsAsync(stoppingToken);
        }

        private void MonitorQuote(Quote quote)
        {
            lock (_lock)
            {
                _totalQuotes++;

                if (!_priceHistory.ContainsKey(quote.Symbol))
                {
                    _priceHistory[quote.Symbol] = new List<decimal>();
                }

                _priceHistory[quote.Symbol].Add(quote.Price);

                // Храним только последние 100 значений
                if (_priceHistory[quote.Symbol].Count > 100)
                {
                    _priceHistory[quote.Symbol].RemoveAt(0);
                }
            }
        }

        private async Task RunPeriodicReportsAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                    lock (_lock)
                    {
                        if (_totalQuotes == 0) continue;

                        _logger.LogInformation(@"
=================== MONITORING REPORT ===================
Всего котировок обработано: {Total}
Уникальных символов: {Symbols}
{Stats}
========================================================",
                            _totalQuotes,
                            _priceHistory.Count,
                            GetStatistics());
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private string GetStatistics()
        {
            var report = new System.Text.StringBuilder();

            foreach (var kvp in _priceHistory)
            {
                var prices = kvp.Value;
                if (prices.Count == 0) continue;

                var avg = prices.Average();
                var min = prices.Min();
                var max = prices.Max();
                var current = prices.Last();

                report.AppendLine($"  {kvp.Key}:");
                report.AppendLine($"    Текущая: {current:C}, Средн: {avg:C}");
                report.AppendLine($"    Мин: {min:C}, Макс: {max:C}");
                report.AppendLine($"    Волатильность: {(max - min) / avg * 100:F2}%");
            }

            return report.ToString();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring Service останавливается. Финальный отчет:");

            lock (_lock)
            {
                _logger.LogInformation("Всего отслежено котировок: {Count}", _totalQuotes);
            }

            _monitoringBlock?.Complete();
            await (_monitoringBlock?.Completion ?? Task.CompletedTask);

            await base.StopAsync(cancellationToken);
        }
    }
}
