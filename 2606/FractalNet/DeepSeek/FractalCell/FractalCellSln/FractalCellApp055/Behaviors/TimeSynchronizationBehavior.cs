// 26.07.12
// 2 errors
// Behaviors/TimeSynchronizationBehavior.fixed.cs

using System.Threading;
using FractalCellCore;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellApp.Behaviors;

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

//using System.Threading;
//using FractalCellCore;
//using FractalCellCore.Core.Interfaces;
//using FractalCellCore.Core.Templates;
//using Microsoft.Extensions.Logging;

//namespace FractalCellApp.Behaviors;

///// <summary>
///// Поведение для синхронизации локального времени с фрактальным временем (исправленная версия)
///// </summary>
//public class TimeSynchronizationBehavior : EventBehaviorTemplate<FractalTimeEvent>
//{
//    private DateTimeOffset _currentTime;
//    private long _lastTick;

//    public override string BehaviorId => "TimeSynchronizationBehavior";
//    public override int Priority => 10;

//    public TimeSynchronizationBehavior(ILogger<TimeSynchronizationBehavior>? logger = null)
//        : base(logger)
//    {
//    }

//    public TimeSynchronizationBehavior() : this(null) { }

//    /// <summary>
//    /// Текущее синхронизированное время (потокобезопасное чтение)
//    /// </summary>
//    public DateTimeOffset CurrentTime => Volatile.Read(ref _currentTime);   // Ошибка:  Volatile.Read !!!!!!!!!!!!!!!! 

//    /// <summary>
//    /// Последний полученный тик (потокобезопасное чтение)
//    /// </summary>
//    public long LastTick => Volatile.Read(ref _lastTick);

//    public override Task<bool> CanHandleAsync(IApplicationEvent @event)
//    {
//        return Task.FromResult(@event is FractalTimeEvent);
//    }

//    protected override async Task HandleEventAsync(FractalTimeEvent @event)
//    {
//        // Потокобезопасная запись
//        Volatile.Write(ref _currentTime, @event.FractalTime);    // Ошибка:  Volatile.Write !!!!!!!!!!!!!!!! 
//        Volatile.Write(ref _lastTick, @event.Tick);

//        // Логируем только каждый 10-й тик или первый
//        if (@event.Tick % 10 == 0 || @event.Tick == 1)
//        {
//            _logger?.LogInformation("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}",
//                @event.FractalTime, @event.Tick);
//        }
//        else
//        {
//            _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}",
//                @event.FractalTime, @event.Tick);
//        }

//        await Task.CompletedTask;
//    }
//}

//// 26.07.08

//using FractalCellCore;
//using FractalCellCore.Core.Interfaces;
//using FractalCellCore.Core.Templates;
//using Microsoft.Extensions.Logging;

//namespace FractalCellApp.Behaviors;

///// <summary>
///// Поведение для синхронизации локального времени с фрактальным временем.
///// </summary>
//public class TimeSynchronizationBehavior : EventBehaviorTemplate<FractalTimeEvent>
//{
//    private DateTimeOffset _currentTime;
//    private long _lastTick;

//    public override string BehaviorId => "TimeSynchronizationBehavior";
//    public override int Priority => 10; // средний приоритет

//    public TimeSynchronizationBehavior(ILogger<TimeSynchronizationBehavior>? logger = null)
//        : base(logger)
//    {
//    }

//    public TimeSynchronizationBehavior() : this(null) { }

//    /// <summary>
//    /// Текущее синхронизированное время (фрактальное)
//    /// </summary>
//    public DateTimeOffset CurrentTime => _currentTime;

//    protected override async Task HandleEventAsync(FractalTimeEvent @event)
//    {
//        _currentTime = @event.FractalTime;
//        _lastTick = @event.Tick;

//        // Логируем только каждый 10-й тик или первый, чтобы не засорять
//        if (_lastTick % 10 == 0 || _lastTick == 1)
//        {
//            _logger?.LogInformation("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);
//        }
//        else
//        {
//            _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);
//        }

//        await Task.CompletedTask;
//    }

//    //protected override async Task HandleEventAsync(FractalTimeEvent @event)
//    //{
//    //    _currentTime = @event.FractalTime;
//    //    _lastTick = @event.Tick;

//    //    _logger?.LogDebug("⏱️ [TimeSync] Time updated to {Time}, tick {Tick}", _currentTime, _lastTick);

//    //    // Здесь можно добавить проверку на пропуски тиков и, если нужно, сгенерировать предупреждение
//    //    await Task.CompletedTask;
//    //}
//}
