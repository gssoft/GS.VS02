// 26.07.12
// Core/Templates/BehaviorTemplate.fixed.cs

using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace FractalCellCore.Core.Templates;

/// <summary>
/// Абстрактный базовый класс для поведения (исправленная версия)
/// </summary>
public abstract class BehaviorTemplate : IBehavior, IDisposable
{
    protected readonly ILogger? _logger;
    protected IFractalCell? _attachedCell;
    protected CancellationTokenSource? _cts;
    protected bool _isAttached;

    public abstract string BehaviorId { get; }

    // Приоритет по умолчанию — средний
    public virtual int Priority => 100;

    protected BehaviorTemplate(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Привязка поведения к ячейке
    /// </summary>
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

    /// <summary>
    /// Отвязка поведения от ячейки
    /// </summary>
    public virtual async Task DetachAsync(CancellationToken ct = default)
    {
        if (!_isAttached)
            return;

        _isAttached = false;

        try
        {
            _cts?.Cancel();
        }
        catch { }

        _cts?.Dispose();
        _cts = null;

        await OnDetachedAsync(ct);

        _logger?.LogInformation("Behavior {BehaviorId} detached", BehaviorId);

        _attachedCell = null;
    }

    /// <summary>
    /// Настройка поведения (теперь можно вызывать ДО AttachToAsync)
    /// </summary>
    public virtual async Task ConfigureAsync(object? configuration = null, CancellationToken ct = default)
    {
        _logger?.LogInformation("Configuring behavior {BehaviorId}", BehaviorId);
        await OnConfigureAsync(configuration, ct);
    }

    /// <summary>
    /// Проверка, может ли поведение обработать событие
    /// </summary>
    public virtual Task<bool> CanHandleAsync(IApplicationEvent @event)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Обработка события
    /// </summary>
    public virtual async Task HandleAsync(IApplicationEvent @event)
    {
        _logger?.LogWarning("Behavior {BehaviorId} received event {EventId} but doesn't handle it",
            BehaviorId, @event.EventId);

        await Task.CompletedTask;
    }

    protected virtual Task OnAttachedAsync(CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnDetachedAsync(CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnConfigureAsync(object? configuration, CancellationToken ct) => Task.CompletedTask;

    public virtual void Dispose()
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }

        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}



