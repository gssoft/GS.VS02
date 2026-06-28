using System.Threading.Tasks.Dataflow;

namespace FractalCell.Core;

/// <summary>
/// 🌳 ФРАКТАЛЬНАЯ КОМПОЗИЦИЯ.
/// Это IFractalCell, который содержит другие IFractalCell.
/// 
/// Свойства:
/// - Для внешнего мира выглядит как обычная ячейка
/// - Внутри содержит дочерние ячейки (которые могут быть тоже композициями!)
/// - Автоматически маршрутизирует события к нужному ребёнку по TargetCellId
/// - Может иметь свои собственные поведения (обрабатываются ДО маршрутизации)
/// 
/// Это позволяет строить ЛЮБЫЕ топологии: деревья, графы, кольца — 
/// всё из одного примитива.
/// </summary>
public class FractalComposition : IFractalCell
{
    public string CellId { get; }

    private readonly FractalCellPipeline _self; // Собственные поведения
    private readonly List<IFractalCell> _children = new();
    private readonly Dictionary<string, IFractalCell> _childMap = new();

    public FractalComposition(string cellId, params IFractalCell[] children)
    {
        CellId = cellId;
        _self = new FractalCellPipeline($"{cellId}-self");

        foreach (var child in children)
            AddChild(child);
    }

    /// <summary>
    /// Динамически добавляем ребёнка. Можно в runtime!
    /// </summary>
    public FractalComposition AddChild(IFractalCell child)
    {
        _children.Add(child);
        _childMap[child.CellId] = child;
        return this;
    }

    /// <summary>
    /// Добавляем собственное поведение (обрабатывается ДО маршрутизации детям)
    /// </summary>
    public FractalComposition WithBehavior(BehaviorBlock behavior)
    {
        _self.WithBehavior(behavior);
        return this;
    }

    /// <summary>
    /// 🎯 СЕРДЦЕ ФРАКТАЛЬНОСТИ: Маршрутизация.
    /// 
    /// Алгоритм:
    /// 1. Если TargetCellId указан И есть ребёнок с таким ID → отправить ребёнку
    /// 2. Если TargetCellId указан, но ребёнка нет → ищем РЕКУРСИВНО в поддереве
    /// 3. Если TargetCellId не указан → обрабатываем сами (собственные поведения)
    /// 
    /// Это позволяет посылать события глубоко во вложенные композиции,
    /// не зная их структуры!
    /// </summary>
    public async Task SendAsync(IApplicationEvent @event, CancellationToken ct = default)
    {
        // 1. Прямой ребёнок?
        if (!string.IsNullOrEmpty(@event.TargetCellId) &&
            _childMap.TryGetValue(@event.TargetCellId, out var directChild))
        {
            await directChild.SendAsync(@event, ct);
            return;
        }

        // 2. Рекурсивный поиск в поддереве
        if (!string.IsNullOrEmpty(@event.TargetCellId))
        {
            var found = FindInChildren(@event.TargetCellId);
            if (found != null)
            {
                await found.SendAsync(@event, ct);
                return;
            }
        }

        // 3. Обрабатываем сами
        await _self.SendAsync(@event, ct);
    }

    private IFractalCell? FindInChildren(string targetId)
    {
        foreach (var child in _children)
        {
            if (child.CellId == targetId)
                return child;

            // Рекурсивно ищем в композициях
            if (child is FractalComposition composition)
            {
                var found = composition.FindInChildren(targetId);
                if (found != null) return found;
            }
        }
        return null;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await _self.StartAsync(ct);
        foreach (var child in _children)
            await child.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        foreach (var child in _children)
            await child.StopAsync(ct);
        await _self.StopAsync(ct);
    }

    /// <summary>
    /// Визуализация структуры (для отладки)
    /// </summary>
    public string Dump(string indent = "")
    {
        var result = $"{indent}📦 {CellId}";
        if (_children.Count > 0)
            result += $" [{_children.Count} children]";
        result += "\n";

        foreach (var child in _children)
        {
            if (child is FractalComposition comp)
                result += comp.Dump(indent + "  ");
            else
                result += $"{indent}  ├─ {child.CellId}\n";
        }
        return result;
    }
}
