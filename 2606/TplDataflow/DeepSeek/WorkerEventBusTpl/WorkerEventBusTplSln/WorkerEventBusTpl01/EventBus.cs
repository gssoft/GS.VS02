//// EventBus.cs

using System.Threading.Tasks.Dataflow;
using WorkerEventBus.Events;
using WorkerEventBus.Handlers;

namespace WorkerEventBus;

public class EventBus : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBus> _logger;
    private readonly Dictionary<Type, List<Type>> _handlers = new();
    private readonly Dictionary<Type, ActionBlock<object>> _executionBlocks = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        RegisterEvent<EventA, HandlerA>();
        RegisterEvent<EventB, HandlerB>();
        RegisterEvent<EventC, HandlerC>();

        InitializeExecutionBlocks();
    }

    private void RegisterEvent<TEvent, THandler>()
        where TEvent : IEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Type>();

        _handlers[eventType].Add(typeof(THandler));
        _logger.LogInformation("Registered {Handler} for {Event}",
            typeof(THandler).Name, eventType.Name);
    }

    private void InitializeExecutionBlocks()
    {
        foreach (var eventType in _handlers.Keys)
        {
            var block = new ActionBlock<object>(async (eventObj) =>
            {
                var handlers = _handlers[eventType];
                foreach (var handlerType in handlers)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                        var method = handlerType.GetMethod("HandleAsync");
                        if (method != null)
                        {
                            var task = (Task)method.Invoke(handler, new[] { eventObj, _cts.Token });
                            await task;
                            _logger.LogInformation("✓ Handler {HandlerType} completed", handlerType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {EventType}", eventType.Name);
                    }
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 100,
                CancellationToken = _cts.Token
            });

            _executionBlocks[eventType] = block;
            _logger.LogInformation("Created execution block for {EventType}", eventType.Name);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);

        if (!_executionBlocks.ContainsKey(eventType))
        {
            _logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation("📨 Publishing {EventType}: {@Event}", eventType.Name, @event);

        var accepted = await _executionBlocks[eventType].SendAsync(@event, _cts.Token);

        if (!accepted)
        {
            _logger.LogWarning("Event {EventId} was not accepted by block", @event.Id);
        }
    }

    public async Task CompleteAsync()
    {
        _logger.LogInformation("Completing EventBus...");

        foreach (var block in _executionBlocks.Values)
        {
            block.Complete();
        }

        await Task.WhenAll(_executionBlocks.Values.Select(b => b.Completion));

        _logger.LogInformation("EventBus completed");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cts.Cancel();
        _cts.Dispose();

        foreach (var block in _executionBlocks.Values)
        {
            block.Complete();
        }

        _disposed = true;
    }
}

//using System.Threading.Tasks.Dataflow;
//using WorkerEventBus.Events;
//using WorkerEventBus.Handlers;

//namespace WorkerEventBus;

//public class EventBus : IDisposable
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<EventBus> _logger;
//    private readonly Dictionary<Type, List<Type>> _handlers = new();
//    private readonly Dictionary<Type, ActionBlock<object>> _executionBlocks = new();
//    private readonly CancellationTokenSource _cts = new();
//    private bool _disposed;

//    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;

//        RegisterEvent<EventA, HandlerA>();
//        RegisterEvent<EventB, HandlerB>();
//        RegisterEvent<EventC, HandlerC>();

//        InitializeExecutionBlocks();
//    }

//    private void RegisterEvent<TEvent, THandler>()
//        where TEvent : IEvent
//        where THandler : IEventHandler<TEvent>
//    {
//        var eventType = typeof(TEvent);
//        if (!_handlers.ContainsKey(eventType))
//            _handlers[eventType] = new List<Type>();

//        _handlers[eventType].Add(typeof(THandler));
//        _logger.LogInformation("Registered {Handler} for {Event}",
//            typeof(THandler).Name, eventType.Name);
//    }

//    private void InitializeExecutionBlocks()
//    {
//        foreach (var eventType in _handlers.Keys)
//        {
//            var block = new ActionBlock<object>(async (eventObj) =>
//            {
//                var handlers = _handlers[eventType];
//                foreach (var handlerType in handlers)
//                {
//                    try
//                    {
//                        using var scope = _serviceProvider.CreateScope();
//                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

//                        var method = handlerType.GetMethod("HandleAsync");
//                        if (method != null)
//                        {
//                            var task = (Task)method.Invoke(handler, new[] { eventObj, _cts.Token });
//                            await task;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, "Error processing event {EventType}", eventType.Name);
//                    }
//                }
//            }, new ExecutionDataflowBlockOptions
//            {
//                MaxDegreeOfParallelism = 1,
//                BoundedCapacity = 100,
//                CancellationToken = _cts.Token
//            });

//            _executionBlocks[eventType] = block;
//            _logger.LogInformation("Created execution block for {EventType}", eventType.Name);
//        }
//    }

//    // Обобщенный метод для строго типизированной публикации
//    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
//    {
//        var eventType = typeof(TEvent);

//        if (!_executionBlocks.ContainsKey(eventType))
//        {
//            _logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
//            return;
//        }

//        _logger.LogInformation("📨 Publishing {EventType}: {@Event}", eventType.Name, @event);

//        var accepted = await _executionBlocks[eventType].SendAsync(@event, _cts.Token);

//        if (!accepted)
//        {
//            _logger.LogWarning("Event {EventId} was not accepted by block", @event.Id);
//        }
//    }

//    // ✅ ПРОСТОЕ РЕШЕНИЕ С DYNAMIC
//    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
//    {
//        // Dynamic вызывает правильный обобщенный метод
//        await ((dynamic)this).PublishAsync((dynamic)@event);
//    }

//    public async Task CompleteAsync()
//    {
//        _logger.LogInformation("Completing EventBus...");

//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        await Task.WhenAll(_executionBlocks.Values.Select(b => b.Completion));

//        _logger.LogInformation("EventBus completed");
//    }

//    public void Dispose()
//    {
//        if (_disposed) return;

//        _cts.Cancel();
//        _cts.Dispose();

//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        _disposed = true;
//    }
//}

//// EventBus.cs - ИСПРАВЛЕННАЯ ВЕРСИЯ
//using System.Threading.Tasks.Dataflow;
//using WorkerEventBus.Events;
//using WorkerEventBus.Handlers;

//namespace WorkerEventBus;

//public class EventBus : IDisposable
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<EventBus> _logger;

//    // Хранилище обработчиков для каждого типа события
//    private readonly Dictionary<Type, List<Type>> _handlers = new();

//    // TPL Dataflow блоки для параллельного исполнения
//    private readonly Dictionary<Type, ActionBlock<object>> _executionBlocks = new();

//    private readonly CancellationTokenSource _cts = new();
//    private bool _disposed;

//    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;

//        // Регистрируем три события с их обработчиками
//        RegisterEvent<EventA, HandlerA>();
//        RegisterEvent<EventB, HandlerB>();
//        RegisterEvent<EventC, HandlerC>();

//        // Создаем параллельные потоки для каждого типа события
//        InitializeExecutionBlocks();
//    }

//    private void RegisterEvent<TEvent, THandler>()
//        where TEvent : IEvent
//        where THandler : IEventHandler<TEvent>
//    {
//        var eventType = typeof(TEvent);
//        if (!_handlers.ContainsKey(eventType))
//            _handlers[eventType] = new List<Type>();

//        _handlers[eventType].Add(typeof(THandler));
//        _logger.LogInformation("Registered {Handler} for {Event}",
//            typeof(THandler).Name, eventType.Name);
//    }

//    private void InitializeExecutionBlocks()
//    {
//        foreach (var eventType in _handlers.Keys)
//        {
//            var block = new ActionBlock<object>(async (eventObj) =>
//            {
//                var handlers = _handlers[eventType];
//                foreach (var handlerType in handlers)
//                {
//                    try
//                    {
//                        using var scope = _serviceProvider.CreateScope();
//                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

//                        var method = handlerType.GetMethod("HandleAsync");
//                        if (method != null)
//                        {
//                            var task = (Task)method.Invoke(handler, new[] { eventObj, _cts.Token });
//                            await task;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, "Error processing event {EventType}", eventType.Name);
//                    }
//                }
//            }, new ExecutionDataflowBlockOptions
//            {
//                MaxDegreeOfParallelism = 1,
//                BoundedCapacity = 100,
//                CancellationToken = _cts.Token
//            });

//            _executionBlocks[eventType] = block;
//            _logger.LogInformation("Created execution block for {EventType}", eventType.Name);
//        }
//    }

//    // Обобщенный метод для строго типизированной публикации
//    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
//    {
//        var eventType = typeof(TEvent);

//        if (!_executionBlocks.ContainsKey(eventType))
//        {
//            _logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
//            return;
//        }

//        _logger.LogInformation("📨 Publishing {EventType}: {@Event}", eventType.Name, @event);

//        var accepted = await _executionBlocks[eventType].SendAsync(@event, _cts.Token);

//        if (!accepted)
//        {
//            _logger.LogWarning("Event {EventId} was not accepted by block", @event.Id);
//        }
//    }

//    // ✅ ИСПРАВЛЕННЫЙ необобщенный метод - используем рефлексию для вызова правильного обобщенного метода
//    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
//    {
//        var eventType = @event.GetType();

//        // Находим обобщенный метод PublishAsync<>
//        var method = typeof(EventBus).GetMethod(nameof(PublishAsync), new[] { typeof(IEvent) });
//        if (method == null)
//        {
//            _logger.LogError("Could not find generic PublishAsync method");
//            return;
//        }

//        // Создаем конкретную версию метода для типа события
//        var genericMethod = method.MakeGenericMethod(eventType);

//        // Вызываем метод
//        var task = (Task)genericMethod.Invoke(this, new[] { @event });
//        await task;
//    }

//    public async Task CompleteAsync()
//    {
//        _logger.LogInformation("Completing EventBus...");

//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        await Task.WhenAll(_executionBlocks.Values.Select(b => b.Completion));

//        _logger.LogInformation("EventBus completed");
//    }

//    public void Dispose()
//    {
//        if (_disposed) return;

//        _cts.Cancel();
//        _cts.Dispose();

//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        _disposed = true;
//    }
//}

//// EventBus.cs
//using System.Threading.Tasks.Dataflow;
//using WorkerEventBus.Events;
//using WorkerEventBus.Handlers;

//namespace WorkerEventBus;

//public class EventBus : IDisposable
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<EventBus> _logger;

//    // Хранилище обработчиков для каждого типа события
//    private readonly Dictionary<Type, List<Type>> _handlers = new();

//    // TPL Dataflow блоки для параллельного исполнения
//    private readonly Dictionary<Type, ActionBlock<object>> _executionBlocks = new();

//    private readonly CancellationTokenSource _cts = new();
//    private bool _disposed;

//    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;

//        // Регистрируем три события с их обработчиками
//        RegisterEvent<EventA, HandlerA>();
//        RegisterEvent<EventB, HandlerB>();
//        RegisterEvent<EventC, HandlerC>();

//        // Создаем параллельные потоки для каждого типа события
//        InitializeExecutionBlocks();
//    }

//    private void RegisterEvent<TEvent, THandler>()
//        where TEvent : IEvent
//        where THandler : IEventHandler<TEvent>
//    {
//        var eventType = typeof(TEvent);
//        if (!_handlers.ContainsKey(eventType))
//            _handlers[eventType] = new List<Type>();

//        _handlers[eventType].Add(typeof(THandler));
//        _logger.LogInformation("Registered {Handler} for {Event}",
//            typeof(THandler).Name, eventType.Name);
//    }

//    // EventBus.cs - исправленная версия
//    private void InitializeExecutionBlocks()
//    {
//        foreach (var eventType in _handlers.Keys)
//        {
//            // Для каждого типа события создаем свой ActionBlock
//            // MaxDegreeOfParallelism = 1 означает, что события ОДНОГО типа 
//            // обрабатываются последовательно, но РАЗНЫЕ типы - параллельно
//            var block = new ActionBlock<object>(async (eventObj) =>
//            {
//                var handlers = _handlers[eventType];
//                foreach (var handlerType in handlers)
//                {
//                    try
//                    {
//                        using var scope = _serviceProvider.CreateScope();
//                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

//                        var method = handlerType.GetMethod("HandleAsync");
//                        if (method != null)
//                        {
//                            var task = (Task)method.Invoke(handler, new[] { eventObj, _cts.Token });
//                            await task;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, "Error processing event {EventType}", eventType.Name);
//                    }
//                }
//            }, new ExecutionDataflowBlockOptions
//            {
//                MaxDegreeOfParallelism = 1,      // ✅ Правильное имя свойства
//                BoundedCapacity = 100,
//                CancellationToken = _cts.Token
//            });

//            _executionBlocks[eventType] = block;
//            _logger.LogInformation("Created execution block for {EventType}", eventType.Name);
//        }
//    }

//    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
//    {
//        var eventType = typeof(TEvent);

//        if (!_executionBlocks.ContainsKey(eventType))
//        {
//            _logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
//            return;
//        }

//        _logger.LogInformation("📨 Publishing {EventType}: {@Event}", eventType.Name, @event);

//        // Отправляем событие в соответствующий TPL Dataflow блок
//        var accepted = await _executionBlocks[eventType].SendAsync(@event, _cts.Token);

//        if (!accepted)
//        {
//            _logger.LogWarning("Event {EventId} was not accepted by block", @event.Id);
//        }
//    }

//    // ✅ Новый необобщенный метод для удобства
//    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
//    {
//        await PublishAsync((dynamic)@event, cancellationToken);
//        // Или используйте рефлексию, если не нравится dynamic
//    }

//    public async Task CompleteAsync()
//    {
//        _logger.LogInformation("Completing EventBus...");

//        // Завершаем все блоки
//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        // Ждем завершения всех блоков
//        await Task.WhenAll(_executionBlocks.Values.Select(b => b.Completion));

//        _logger.LogInformation("EventBus completed");
//    }

//    public void Dispose()
//    {
//        if (_disposed) return;

//        _cts.Cancel();
//        _cts.Dispose();

//        foreach (var block in _executionBlocks.Values)
//        {
//            block.Complete();
//        }

//        _disposed = true;
//    }
//}
