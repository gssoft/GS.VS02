// CompositeKeyRouter.cs (исправленная версия)

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BusLibrary;

/// <summary>
/// Композитный роутер, объединяющий атрибутные и динамические подписки
/// </summary>
public sealed class CompositeKeyRouter : IKeyRouter
{
    private readonly IEventHandlerRegistry _registry;

    // Updates 26.05.22
    private readonly IDynamicSubscriptionManager _subscriptionManager;
    private readonly ILogger<CompositeKeyRouter>? _logger;

    public CompositeKeyRouter(
        IEventHandlerRegistry registry,
        // Updates 26.05.22
        IDynamicSubscriptionManager subscriptionManager,
        ILogger<CompositeKeyRouter>? logger = null)
    {
        _registry = registry;
        _subscriptionManager = subscriptionManager;
        _logger = logger;
    }

    public IEnumerable<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>
        Resolve(IServiceProvider serviceProvider, string key)
    {
        var handlers = new List<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>();

        // 1. Статические обработчики из атрибутов
        var staticHandlers = GetStaticHandlers(serviceProvider, key);
        handlers.AddRange(staticHandlers);

        _logger?.LogDebug("Found {Count} static handlers for key '{Key}'",
            staticHandlers.Count(), key);

        // 2. Динамические обработчики
        var dynamicHandlers = _subscriptionManager.GetHandlers(key);
        handlers.AddRange(dynamicHandlers);

        _logger?.LogDebug("Found {Count} dynamic handlers for key '{Key}'",
            dynamicHandlers.Count(), key);

        return handlers;
    }

    private IEnumerable<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>
        GetStaticHandlers(IServiceProvider serviceProvider, string key)
    {
        var handlerMap = _registry.GetHandlerMap();
        if (!handlerMap.TryGetValue(key, out var handlerTypes))
            return Enumerable.Empty<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>();

        var invokers = new List<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>();
        foreach (var handlerType in handlerTypes)
        {
            invokers.Add((sp, msg, ct) =>
            {
                var handler = sp.GetRequiredService(handlerType);
                var handleMethod = handlerType.GetMethod("Handle");
                if (handleMethod == null)
                    throw new InvalidOperationException($"Handler {handlerType.Name} does not have Handle method");

                return (ValueTask)handleMethod.Invoke(handler, new object[] { msg, ct });
            });
        }
        return invokers;
    }
}
