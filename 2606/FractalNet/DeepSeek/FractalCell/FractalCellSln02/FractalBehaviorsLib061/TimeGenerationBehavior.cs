using FractalCellCore;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalBehaviors;

/// <summary>
/// Поведение, генерирующее фрактальное время и рассылающее его всем ячейкам.
/// </summary>
public class TimeGenerationBehavior : BackgroundBehaviorTemplate, ILifecycleBehavior
{
    private readonly TimeSpan _interval;
    private long _tick;
    private DateTimeOffset _currentFractalTime;

    public override string BehaviorId => "TimeGenerationBehavior";
    public override int Priority => 5;

    public TimeGenerationBehavior(
        ILogger<TimeGenerationBehavior>? logger = null,
        TimeSpan? interval = null)
        : base(logger)
    {
        _interval = interval ?? TimeSpan.FromSeconds(1);
    }

    public TimeGenerationBehavior() : this(null, null) { }

    protected override async Task BackgroundLoopAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏰ TimeGenerationBehavior started, interval: {Interval}", _interval);


        // Это уже есть
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, ct);

                //if(_attachedCell != null)
                //    _logger?.LogWarning("TimeGenerationBehavior: attached cell !!!");

                if (_attachedCell == null)
                {
                    _logger?.LogWarning("TimeGenerationBehavior: no attached cell, skipping");
                    continue;
                }

                Interlocked.Increment(ref _tick);
                _currentFractalTime = DateTimeOffset.UtcNow; // прямое использование

                var timeEvent = new FractalTimeEvent(
                    eventId: $"ft-{Guid.NewGuid():N}",
                    timestamp: DateTime.UtcNow,
                    sourceCellId: _attachedCell.CellId,
                    fractalTime: _currentFractalTime,
                    tick: _tick,
                    isSynchronized: true
                );

                _logger?.LogInformation("⏰ [TimeGen] Tick {Tick} at {Time}", _tick, _currentFractalTime);

                await _attachedCell.ExternalBus.BroadcastAsync(timeEvent);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TimeGenerationBehavior error");
            }
        }

        _logger?.LogInformation("🏁 TimeGenerationBehavior stopped, total ticks: {Tick}", _tick);
    }

    public async Task OnCellStartingAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏰ TimeGeneration: cell starting, resetting tick counter");
        Interlocked.Exchange(ref _tick, 0);
        await Task.CompletedTask;
    }

    public async Task OnCellStartedAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏰ TimeGeneration: cell started");
        await Task.CompletedTask;
    }

    public async Task OnCellStoppingAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏰ TimeGeneration: cell stopping, final tick: {Tick}", _tick);
        await Task.CompletedTask;
    }

    public async Task OnCellStoppedAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏰ TimeGeneration: cell stopped");
        await Task.CompletedTask;
    }

    // 26.07.10
    public override Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        // Это поведение только генерирует события, но не обрабатывает входящие
        return Task.FromResult(false);
    }
}
