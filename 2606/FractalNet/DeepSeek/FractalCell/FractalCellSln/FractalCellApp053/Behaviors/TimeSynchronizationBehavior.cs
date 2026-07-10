using FractalCellCore;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellApp.Behaviors;

/// <summary>
/// Поведение для синхронизации локального времени с фрактальным временем.
/// </summary>
public class TimeSynchronizationBehavior : EventBehaviorTemplate<FractalTimeEvent>
{
    private DateTimeOffset _currentTime;
    private long _lastTick;

    public override string BehaviorId => "TimeSynchronizationBehavior";
    public override int Priority => 10; // средний приоритет

    public TimeSynchronizationBehavior(ILogger<TimeSynchronizationBehavior>? logger = null)
        : base(logger)
    {
    }

    public TimeSynchronizationBehavior() : this(null) { }

    /// <summary>
    /// Текущее синхронизированное время (фрактальное)
    /// </summary>
    public DateTimeOffset CurrentTime => _currentTime;

    protected override async Task HandleEventAsync(FractalTimeEvent @event)
    {
        _currentTime = @event.FractalTime;
        _lastTick = @event.Tick;

        // Логируем только каждый 10-й тик или первый, чтобы не засорять
        if (_lastTick % 10 == 0 || _lastTick == 1)
        {
            _logger?.LogInformation("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);
        }
        else
        {
            _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);
        }

        await Task.CompletedTask;
    }

    //protected override async Task HandleEventAsync(FractalTimeEvent @event)
    //{
    //    _currentTime = @event.FractalTime;
    //    _lastTick = @event.Tick;

    //    _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);

    //    // Здесь можно добавить проверку на пропуски тиков и, если нужно, сгенерировать предупреждение
    //    await Task.CompletedTask;
    //}
}
