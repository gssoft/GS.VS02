// Updates 26.05.22

using BusLibrary;

using Microsoft.Extensions.DependencyInjection;

namespace BusLibrary;

public sealed class StaticKeyRouter : IKeyRouter
{
    private readonly IEventHandlerRegistry _registry;

    public StaticKeyRouter(IEventHandlerRegistry registry) => _registry = registry;

    public IEnumerable<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>
        Resolve(IServiceProvider serviceProvider, string key)
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
