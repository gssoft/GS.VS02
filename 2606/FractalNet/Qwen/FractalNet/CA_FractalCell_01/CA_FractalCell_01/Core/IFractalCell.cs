namespace FractalCell.Core;

/// <summary>
/// Фрактальная ячейка — базовая единица системы.
/// Может быть как простой ячейкой, так и композицией других ячеек.
/// Для внешнего мира ВСЕГДА выглядит одинаково.
/// </summary>
public interface IFractalCell
{
    string CellId { get; }

    /// <summary>
    /// Отправить событие в ячейку. 
    /// Ячейка сама решит: обработать самой или маршрутизировать в детей.
    /// </summary>
    Task SendAsync(IApplicationEvent @event, CancellationToken ct = default);

    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

public interface IApplicationEvent
{
    string EventId { get; }
    string? TargetCellId { get; }  // Кому адресовано (null = всем/себе)
}
