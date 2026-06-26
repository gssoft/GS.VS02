// Core/Templates/BehaviorTemplate.cs

using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FractalCellCore.Core.Templates;

/// <summary>
/// Абстрактный базовый класс для поведения
/// </summary>
public abstract class BehaviorTemplate : IBehavior
{
    protected readonly ILogger? _logger;
    protected IFractalCell? _attachedCell;
    protected CancellationTokenSource? _cts;
    protected bool _isAttached;

    public abstract string BehaviorId { get; }

    protected BehaviorTemplate(ILogger? logger = null)
    {
        _logger = logger;
    }

    public virtual async Task AttachToAsync(IFractalCell cell, CancellationToken ct = default)
    {
        if (_isAttached)
            throw new InvalidOperationException($"Behavior {BehaviorId} is already attached");

        _attachedCell = cell;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _isAttached = true;

        _logger?.LogInformation("Behavior {BehaviorId} attached to cell {CellId}",
            BehaviorId, cell.CellId);

        await OnAttachedAsync(ct);
    }

    public virtual async Task DetachAsync(CancellationToken ct = default)
    {
        if (!_isAttached)
            return;

        _isAttached = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        await OnDetachedAsync(ct);

        _logger?.LogInformation("Behavior {BehaviorId} detached", BehaviorId);
        _attachedCell = null;
    }

    public virtual async Task ConfigureAsync(object? configuration = null, CancellationToken ct = default)
    {
        if (!_isAttached)
            throw new InvalidOperationException($"Behavior {BehaviorId} is not attached");

        _logger?.LogInformation("Configuring behavior {BehaviorId}", BehaviorId);
        await OnConfigureAsync(configuration, ct);
    }

    protected virtual Task OnAttachedAsync(CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnDetachedAsync(CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnConfigureAsync(object? configuration, CancellationToken ct) => Task.CompletedTask;

    public virtual void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Базовое поведение с поддержкой событий
/// </summary>
public abstract class EventBehaviorTemplate<TEvent> : BehaviorTemplate, IEventHandlingBehavior
    where TEvent : IApplicationEvent
{
    protected EventBehaviorTemplate(ILogger? logger = null) : base(logger)
    {
    }

    public virtual async Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        return @event is TEvent;
    }

    public virtual async Task HandleAsync(IApplicationEvent @event)
    {
        if (@event is TEvent typedEvent)
        {
            _logger?.LogDebug("Handling event {EventId} in behavior {BehaviorId}",
                @event.EventId, BehaviorId);
            await HandleEventAsync(typedEvent);
        }
    }

    protected abstract Task HandleEventAsync(TEvent @event);
}

/// <summary>
/// Поведение с поддержкой фоновой работы
/// </summary>
public abstract class BackgroundBehaviorTemplate : BehaviorTemplate
{
    private Task? _backgroundTask;

    protected BackgroundBehaviorTemplate(ILogger? logger = null) : base(logger)
    {
    }

    protected override async Task OnAttachedAsync(CancellationToken ct)
    {
        _backgroundTask = Task.Run(async () => await BackgroundLoopAsync(ct), ct);
        await base.OnAttachedAsync(ct);
    }

    protected override async Task OnDetachedAsync(CancellationToken ct)
    {
        if (_backgroundTask != null && !_backgroundTask.IsCompleted)
        {
            try
            {
                await _backgroundTask.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
        }
        await base.OnDetachedAsync(ct);
    }

    protected abstract Task BackgroundLoopAsync(CancellationToken ct);
}
