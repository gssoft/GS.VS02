// 26.07.12
// 2 errors
// Behaviors/TimeSynchronizationBehavior.fixed.cs

using System.Threading;
using FractalCellCore;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalBehaviors;

public class TimeSynchronizationBehavior : EventBehaviorTemplate<FractalTimeEvent>
{
    private DateTimeOffset _currentTime;
    private long _lastTick;

    // fix: добавляем объект блокировки для потокобезопасного доступа к DateTimeOffset
    private readonly object _timeLock = new();

    public override string BehaviorId => "TimeSynchronizationBehavior";
    public override int Priority => 10;

    public TimeSynchronizationBehavior(ILogger<TimeSynchronizationBehavior>? logger = null)
        : base(logger)
    {
    }

    public TimeSynchronizationBehavior() : this(null) { }

    /// <summary>
    /// Текущее синхронизированное время (потокобезопасное чтение)
    /// </summary>
    public DateTimeOffset CurrentTime
    {
        get
        {
            lock (_timeLock)
            {
                return _currentTime;
            }
        }
    }

    /// <summary>
    /// Последний полученный тик (потокобезопасное чтение)
    /// </summary>
    public long LastTick => Interlocked.Read(ref _lastTick);  // long можно читать через Interlocked

    public override Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        return Task.FromResult(@event is FractalTimeEvent);
    }

    protected override async Task HandleEventAsync(FractalTimeEvent @event)
    {
        // fix: потокобезопасная запись DateTimeOffset через lock
        lock (_timeLock)
        {
            _currentTime = @event.FractalTime;
        }
        Interlocked.Exchange(ref _lastTick, @event.Tick);   // long можно обновлять через Interlocked

        if (@event.Tick % 10 == 0 || @event.Tick == 1)
        {
            _logger?.LogInformation("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}",
                @event.FractalTime, @event.Tick);
        }
        else
        {
            _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}",
                @event.FractalTime, @event.Tick);
        }

        await Task.CompletedTask;
    }
}

