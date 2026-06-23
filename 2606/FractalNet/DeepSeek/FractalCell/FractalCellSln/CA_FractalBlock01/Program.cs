using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

// --- ИНТЕРФЕЙСЫ И КЛАССЫ СОБЫТИЙ ---

/// <summary>
/// Базовый контракт для всех событий приложения.
/// </summary>
public interface IApplicationEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
    string SourceBlockId { get; }
    string TargetBlockId { get; }
    string EventType { get; }
}

/// <summary>
/// Универсальный класс события с данными произвольного типа.
/// </summary>
public class FractalEvent : IApplicationEvent
{
    public string EventId { get; }
    public DateTime Timestamp { get; }
    public string SourceBlockId { get; }
    public string TargetBlockId { get; }
    public string EventType { get; }
    public object? Data { get; }

    public FractalEvent(
        string eventId,
        DateTime timestamp,
        string sourceBlockId,
        string targetBlockId,
        string eventType,
        object? data = null)
    {
        EventId = eventId;
        Timestamp = timestamp;
        SourceBlockId = sourceBlockId;
        TargetBlockId = targetBlockId;
        EventType = eventType;
        Data = data;
    }
}

// --- ФРАКТАЛЬНЫЙ БЛОК ---

/// <summary>
/// Единый блок для всех уровней фрактала. Обрабатывает события и маршрутизирует их дочерним блокам.
/// </summary>
public class FractalBlock<T> where T : IApplicationEvent
{
    private readonly ActionBlock<T> _actionBlock;
    private readonly Dictionary<Type, List<Func<T, Task>>> _handlers = new();
    private readonly List<FractalBlock<T>> _children = new();
    private readonly string _blockId;

    /// <param name="blockId">Уникальный идентификатор блока.</param>
    /// <param name="capacity">Емкость очереди (backpressure).</param>
    /// <param name="maxParallelism">Максимальная степень параллелизма обработки.</param>
    public FractalBlock(string blockId, int capacity = 1000, int maxParallelism = 1)
    {
        _blockId = blockId;
        _actionBlock = new ActionBlock<T>(
            async @event => await ProcessEventAsync(@event),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxParallelism
            });
    }

    /// <summary>
    /// Подписка на конкретный тип события.
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : T
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<T, Task>>();

        // Добавляем обработчик с кастом к нужному типу
        _handlers[eventType].Add(e => handler((TEvent)e));
    }

    /// <summary>
    /// Отправка события в этот блок.
    /// </summary>
    public async Task SendAsync(T @event)
    {
        await _actionBlock.SendAsync(@event);
    }

    /// <summary>
    /// Связывание с дочерним блоком (фрактальность).
    /// </summary>
    public void LinkTo(FractalBlock<T> childBlock)
    {
        _children.Add(childBlock);
    }

    private async Task ProcessEventAsync(T @event)
    {
        Console.WriteLine($"[{_blockId}] Событие '{@event.EventType}' получено.");

        // 1. Локальная обработка
        var eventType = @event.GetType();
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            // Запускаем все обработчики для этого типа события параллельно
            await Task.WhenAll(handlers.Select(h => h(@event)));
        }

        // 2. Маршрутизация в дочерние блоки
        foreach (var child in _children)
        {
            // Простая маршрутизация: если TargetBlockId не указан или совпадает с ID ребенка
            if (string.IsNullOrEmpty(@event.TargetBlockId) || @event.TargetBlockId == child._blockId)
            {
                Console.WriteLine($"[{_blockId}] Пересылка события '{@event.EventType}' в '{child._blockId}'.");
                await child.SendAsync(@event);
            }
        }
    }

    public void Complete() => _actionBlock.Complete();
    public Task Completion => _actionBlock.Completion;
}

// --- ПРОГРАММНАЯ ЛОГИКА ---

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Запуск фрактальной системы обработки событий...");
        Console.WriteLine("Нажмите любую клавишу для остановки...\n");

        // Создаем блоки нашей архитектуры
        var producerBlock = new FractalBlock<IApplicationEvent>("Producer", capacity: 500);
        var routerBlock = new FractalBlock<IApplicationEvent>("Router", capacity: 500);
        var consumerBlock = new FractalBlock<IApplicationEvent>("Consumer", capacity: 500);

        // Настраиваем связи между блоками (фрактальную структуру)
        producerBlock.LinkTo(routerBlock); // Producer -> Router
        routerBlock.LinkTo(consumerBlock); // Router -> Consumer

        // Регистрируем обработчики событий
        consumerBlock.Subscribe<FractalEvent>(async e =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[CONSUMER] Обработано событие: {e.EventType}. Данные: {(e.Data ?? "нет данных")}");
            Console.ResetColor();
        });

        // Задача-генератор событий
        var cts = new CancellationTokenSource();
        var producerTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var evt = new FractalEvent(
                    eventId: Guid.NewGuid().ToString(),
                    timestamp: DateTime.UtcNow,
                    sourceBlockId: "Producer",
                    targetBlockId: "Consumer", // Явно указываем конечного получателя
                    eventType: "DataGenerated",
                    data: $"Тестовые данные #{new Random().Next(1, 100)}"
                );

                try
                {
                    await producerBlock.SendAsync(evt);
                    Console.WriteLine("[PRODUCER] Событие создано и отправлено.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки: {ex.Message}");
                }

                await Task.Delay(1500, cts.Token); // Пауза перед созданием нового события
            }
        }, cts.Token);

        // Ожидание команды на завершение
        Console.ReadKey(true);
        Console.WriteLine("\nПолучена команда на остановку. Завершение работы...");

        // Сигнализируем генератору о завершении
        cts.Cancel();
        await producerTask; // Ждем завершения задачи-производителя

        // Корректное завершение блоков (Graceful Shutdown)
        producerBlock.Complete();
        routerBlock.Complete();
        consumerBlock.Complete();

        // Ждем, пока все блоки обработают свои очереди
        await Task.WhenAll(producerBlock.Completion, routerBlock.Completion, consumerBlock.Completion);

        Console.WriteLine("Все блоки завершили работу. Программа закрыта.");
    }
}

