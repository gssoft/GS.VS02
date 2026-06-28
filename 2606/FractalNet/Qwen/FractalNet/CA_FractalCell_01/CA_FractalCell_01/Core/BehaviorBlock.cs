using System.Threading.Tasks.Dataflow;

namespace FractalCell.Core;

/// <summary>
/// Базовый класс для поведения. Поведение ПРЕВРАЩАЕТСЯ в TransformBlock.
/// </summary>
public abstract class BehaviorBlock
{
    public string BehaviorId { get; }
    public int MaxParallelism { get; }

    protected BehaviorBlock(string behaviorId, int maxParallelism = 1)
    {
        BehaviorId = behaviorId;
        MaxParallelism = maxParallelism;
    }

    /// <summary>
    /// Решает, обрабатывать ли это событие. Если false — событие пропускается дальше.
    /// </summary>
    public virtual bool CanHandle(IApplicationEvent @event) => true;

    /// <summary>
    /// Логика обработки. Возвращает событие (возможно изменённое) дальше по pipeline.
    /// </summary>
    protected abstract Task<IApplicationEvent> ProcessAsync(
        IApplicationEvent @event, IFractalCell hostCell, CancellationToken ct);

    /// <summary>
    /// Фабрика Dataflow-блока. Вызывается ячейкой при подключении.
    /// </summary>
    internal TransformBlock<IApplicationEvent, IApplicationEvent> CreateBlock(IFractalCell cell)
    {
        return new TransformBlock<IApplicationEvent, IApplicationEvent>(
            async @event =>
            {
                if (!CanHandle(@event))
                    return @event; // Пропускаем дальше

                try
                {
                    return await ProcessAsync(@event, cell, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{BehaviorId}] Error: {ex.Message}");
                    return @event;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                BoundedCapacity = 100,
                NameFormat = $"{cell.CellId}-{BehaviorId}-{{0}}"
            });
    }
}
