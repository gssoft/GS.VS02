// EventBus.cs

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
            // Определяем оптимальный DOP для каждого типа
            var dop = eventType.Name switch
            {
                "EventA" => 20,  // HandlerA: 1000ms -> 20 параллельных
                "EventB" => 30,  // HandlerB: 1500ms -> 30 параллельных  
                "EventC" => 15,  // HandlerC: 800ms -> 15 параллельных
                _ => 10
            };

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
                            _logger.LogDebug("✓ Handler {HandlerType} completed", handlerType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {EventType}", eventType.Name);
                    }
                }
            }, 
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 20,
                // MaxDegreeOfParallelism = dop,     // ✅ Динамический DOP
                // BoundedCapacity = 1000,           // ✅ Увеличен до 1000
                BoundedCapacity = 500,
                CancellationToken = _cts.Token
            });

            _executionBlocks[eventType] = block;
            _logger.LogInformation("Created execution block for {EventType} with MaxDOP={DOP}", eventType.Name, dop);
        }
    }

   

    // Добавьте в EventBus.cs
    public void ClearQueues()
    {
        _logger.LogWarning("⚠️ Clearing all queues!");

        foreach (var block in _executionBlocks.Values)
        {
            // Пропускаем все ожидающие сообщения
            while (block.InputCount > 0)
            {
                // Принудительно очищаем
            }
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

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

        var eventType = typeof(TEvent);

        if (!_executionBlocks.ContainsKey(eventType))
        {
            _logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation("📨 Publishing {EventType}: {@Event}", eventType.Name, @event);

        var accepted = await _executionBlocks[eventType].SendAsync(@event, linkedCts.Token);

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

    public (int QueueA, int QueueB, int QueueC) GetStats()
    {
        var queueA = _executionBlocks.ContainsKey(typeof(EventA))
            ? _executionBlocks[typeof(EventA)].InputCount
            : 0;

        var queueB = _executionBlocks.ContainsKey(typeof(EventB))
            ? _executionBlocks[typeof(EventB)].InputCount
            : 0;

        var queueC = _executionBlocks.ContainsKey(typeof(EventC))
            ? _executionBlocks[typeof(EventC)].InputCount
            : 0;

        return (queueA, queueB, queueC);
    }

    public (int InputCountA, int InputCountB, int InputCountC) GetBlockStats()
    {
        var inputA = _executionBlocks.ContainsKey(typeof(EventA))
            ? _executionBlocks[typeof(EventA)].InputCount
            : 0;

        var inputB = _executionBlocks.ContainsKey(typeof(EventB))
            ? _executionBlocks[typeof(EventB)].InputCount
            : 0;

        var inputC = _executionBlocks.ContainsKey(typeof(EventC))
            ? _executionBlocks[typeof(EventC)].InputCount
            : 0;

        return (inputA, inputB, inputC);
    }

    public Dictionary<string, object> GetDetailedBlockInfo()
    {
        var info = new Dictionary<string, object>();

        foreach (var kvp in _executionBlocks)
        {
            var block = kvp.Value;
            info[$"{kvp.Key.Name}_InputCount"] = block.InputCount;
            info[$"{kvp.Key.Name}_CompletionStatus"] = block.Completion.IsCompleted;
        }

        return info;
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
