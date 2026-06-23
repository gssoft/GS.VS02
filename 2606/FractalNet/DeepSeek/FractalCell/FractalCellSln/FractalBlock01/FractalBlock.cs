using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Text;

// ЕДИНЫЙ блок для всех уровней фрактала
public class FractalBlock<T> where T : IApplicationEvent
{
    private readonly ActionBlock<T> _actionBlock;
    private readonly Dictionary<Type, List<Func<T, Task>>> _handlers = new();
    private readonly List<FractalBlock<T>> _children = new();
    private readonly string _blockId;

    public FractalBlock(string blockId, int capacity = 1000, int maxParallelism = 1)
    {
        _blockId = blockId;
        _actionBlock = new ActionBlock<T>(
            async @event => await ProcessEventAsync(@event),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxParallelism
            }
        );
    }

    // Подписка на события (как MicroEventBus)
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : T
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<T, Task>>();

        _handlers[eventType].Add(e => handler((TEvent)e));
    }

    // Отправка события в этот блок (как Channel.Writer)
    public async Task SendAsync(T @event)
    {
        await _actionBlock.SendAsync(@event);
    }

    // Связывание с дочерним блоком (фрактальность)
    public void LinkTo(FractalBlock<T> childBlock, Predicate<T>? filter = null)
    {
        _children.Add(childBlock);
    }

    private async Task ProcessEventAsync(T @event)
    {
        // 1. Локальная обработка
        if (_handlers.TryGetValue(@event.GetType(), out var handlers))
        {
            await Task.WhenAll(handlers.Select(h => h(@event)));
        }

        // 2. Маршрутизация в дочерние блоки (фрактальность)
        foreach (var child in _children)
        {
            await child.SendAsync(@event);
        }
    }

    public void Complete() => _actionBlock.Complete();
    public Task Completion => _actionBlock.Completion;
}

