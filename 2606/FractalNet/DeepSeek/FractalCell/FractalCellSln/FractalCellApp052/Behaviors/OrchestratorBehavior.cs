using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellApp.Behaviors;

public class OrchestratorBehavior : BackgroundBehaviorTemplate
{
    private readonly Random _random = new();
    private readonly TimeSpan _interval;
    private int _counter;

    public override string BehaviorId => "OrchestratorBehavior";
    public override int Priority => 5; // высокий приоритет (опционально)

    public OrchestratorBehavior(ILogger<OrchestratorBehavior>? logger = null, TimeSpan? interval = null)
        : base(logger)
    {
        _interval = interval ?? TimeSpan.FromSeconds(3);
    }

    public OrchestratorBehavior() : this(null, null) { }

    public override Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        // Оркестратор только отправляет, но не обрабатывает входящие события
        return Task.FromResult(false);
    }

    protected override async Task BackgroundLoopAsync(CancellationToken ct)
    {
        _logger?.LogInformation("🎛️ OrchestratorBehavior started, interval: {Interval}", _interval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, ct);

                if (_attachedCell == null)
                {
                    _logger?.LogWarning("OrchestratorBehavior: no attached cell, skipping");
                    continue;
                }

                // Выбираем случайный тип события
                var eventTypes = new[] { "Heartbeat", "ProcessData" };
                var eventType = eventTypes[_random.Next(eventTypes.Length)];

                object eventData = eventType == "ProcessData"
                    ? new { Value = _random.Next(100), Priority = _random.Next(1, 5) }
                    : new { Interval = _random.Next(1, 10) };

                var @event = new FractalEvent(
                    $"{eventType.ToLower()}-{DateTime.UtcNow.Ticks}",
                    DateTime.UtcNow,
                    _attachedCell.CellId,
                    string.Empty, // Broadcast – не указываем конкретную цель
                    eventType,
                    new
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = "OrchestratorBehavior",
                        Data = eventData
                    }
                );

                _logger?.LogInformation("📡 [Orchestrator] Broadcasting {EventType} from {CellId}",
                    eventType, _attachedCell.CellId);

                // Отправляем всем через внешнюю шину
                await _attachedCell.ExternalBus.BroadcastAsync(@event);
                _counter++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OrchestratorBehavior error");
            }
        }

        _logger?.LogInformation("🏁 OrchestratorBehavior stopped, total events sent: {Counter}", _counter);
    }
}
