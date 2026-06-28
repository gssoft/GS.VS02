// Core/Interfaces/IBehavior.cs

using FractalCellCore.Core.Interfaces;

namespace FractalCellCore.Core.Interfaces;

/// <summary>
/// Базовый интерфейс для поведения ячейки
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Уникальный идентификатор поведения
    /// </summary>
    string BehaviorId { get; }

    /// <summary>
    /// Приоритет обработки (чем меньше число, тем выше приоритет)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Привязка поведения к ячейке
    /// </summary>
    Task AttachToAsync(IFractalCell cell, CancellationToken ct = default);

    /// <summary>
    /// Отвязка поведения от ячейки
    /// </summary>
    Task DetachAsync(CancellationToken ct = default);

    /// <summary>
    /// Настройка поведения
    /// </summary>
    Task ConfigureAsync(object? configuration = null, CancellationToken ct = default);

    /// <summary>
    /// Проверка, может ли поведение обработать событие
    /// </summary>
    Task<bool> CanHandleAsync(IApplicationEvent @event);

    /// <summary>
    /// Обработка события
    /// </summary>
    Task HandleAsync(IApplicationEvent @event);
}

/// <summary>
/// Поведение с поддержкой жизненного цикла
/// </summary>
public interface ILifecycleBehavior : IBehavior
{
    Task OnCellStartingAsync(CancellationToken ct);
    Task OnCellStartedAsync(CancellationToken ct);
    Task OnCellStoppingAsync(CancellationToken ct);
    Task OnCellStoppedAsync(CancellationToken ct);
}

/// <summary>
/// Поведение с поддержкой ошибок
/// </summary>
public interface IErrorHandlingBehavior : IBehavior
{
    Task OnErrorAsync(Exception ex, IApplicationEvent? @event = null);
}

// 26/06/28
/// <summary>
/// Базовый интерфейс для поведения ячейки
/// </summary>
//public interface IBehavior
//{
//    /// <summary>
//    /// Уникальный идентификатор поведения
//    /// </summary>
//    string BehaviorId { get; }

//    /// <summary>
//    /// Привязка поведения к ячейке
//    /// </summary>
//    Task AttachToAsync(IFractalCell cell, CancellationToken ct = default);

//    /// <summary>
//    /// Отвязка поведения от ячейки
//    /// </summary>
//    Task DetachAsync(CancellationToken ct = default);

//    /// <summary>
//    /// Настройка поведения
//    /// </summary>
//    Task ConfigureAsync(object? configuration = null, CancellationToken ct = default);
//}

///// <summary>
///// Поведение с поддержкой жизненного цикла
///// </summary>
//public interface ILifecycleBehavior : IBehavior
//{
//    Task OnCellStartingAsync(CancellationToken ct);
//    Task OnCellStartedAsync(CancellationToken ct);
//    Task OnCellStoppingAsync(CancellationToken ct);
//    Task OnCellStoppedAsync(CancellationToken ct);
//}

/// <summary>
/// Поведение с поддержкой обработки событий
/// </summary>
/// 26.06.28
//public interface IEventHandlingBehavior : IBehavior
//{
//    Task<bool> CanHandleAsync(IApplicationEvent @event);
//    Task HandleAsync(IApplicationEvent @event);
//}

/// <summary>
/// Поведение с поддержкой ошибок
/// </summary>
//public interface IErrorHandlingBehavior : IBehavior
//{
//    Task OnErrorAsync(Exception ex, IApplicationEvent? @event = null);
//}
