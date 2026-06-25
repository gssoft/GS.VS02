// Шаг 6: Поведение — Генератор Котировок
// Создайте файл Trading/Behaviors/QuoteGeneratorBehavior.cs:

using FractalCell02.Core.Behaviors;
using FractalCell02.Core.Interfaces;
using FractalCell02.Trading.Events;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Trading.Behaviors;

/// <summary>
/// Генератор котировок: каждые N мс генерирует новый тик с Random Walk
/// и рассылает всем через Broadcast
/// </summary>
public class QuoteGeneratorBehavior : ICellBehavior
{
    private readonly string _symbol;
    private decimal _currentMidPrice;
    private readonly decimal _spread;
    private readonly int _intervalMs;
    private readonly Random _random = new();

    public QuoteGeneratorBehavior(
        string symbol,
        decimal startPrice,
        decimal spread = 0.05m,
        int intervalMs = 500)
    {
        _symbol = symbol;
        _currentMidPrice = startPrice;
        _spread = spread;
        _intervalMs = intervalMs;
    }

    public Task OnStartAsync(ICellContext context)
    {
        context.Logger.LogInformation(
            "📈 QuoteGenerator started for {Symbol} at {Price}",
            _symbol, _currentMidPrice);

        // Запускаем фоновую генерацию тиков
        _ = Task.Run(async () =>
        {
            while (!context.StoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Random Walk: движение цены ±0.50
                    var delta = (decimal)(_random.NextDouble() - 0.5) * 1.0m;
                    _currentMidPrice = Math.Max(1.0m, _currentMidPrice + delta);

                    var bid = Math.Round(_currentMidPrice - _spread, 2);
                    var ask = Math.Round(_currentMidPrice + _spread, 2);

                    var quote = new QuoteEvent(
                        EventId: $"quote-{Guid.NewGuid():N}"[..16],
                        Timestamp: DateTime.UtcNow,
                        Symbol: _symbol,
                        Bid: bid,
                        Ask: ask,
                        SourceCellId: context.CellId);

                    // Broadcast всем ячейкам
                    await context.ExternalBus.BroadcastAsync(quote);

                    context.Logger.LogDebug(
                        "📈 [{Symbol}] Bid={Bid} Ask={Ask}",
                        _symbol, bid, ask);

                    await Task.Delay(_intervalMs, context.StoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, "Error generating quote");
                    await Task.Delay(1000, context.StoppingToken);
                }
            }
        }, context.StoppingToken);

        return Task.CompletedTask;
    }

    // Генератор не реагирует на входящие события
    public Task OnMessageAsync(IApplicationEvent @event, ICellContext context)
        => Task.CompletedTask;

    public Task OnStopAsync(ICellContext context)
    {
        context.Logger.LogInformation("📈 QuoteGenerator stopped");
        return Task.CompletedTask;
    }
}
