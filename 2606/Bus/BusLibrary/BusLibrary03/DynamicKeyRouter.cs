// Updates 26.05.22

using BusLibrary;
using Microsoft.Extensions.DependencyInjection;

namespace BusLibrary;

//public sealed class DynamicKeyRouter : IKeyRouter
//{
//    private readonly DynamicSubscriptionManager _manager;
//    public DynamicKeyRouter(DynamicSubscriptionManager manager) => _manager = manager;

//    public IEnumerable<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>
//    Resolve(IServiceProvider serviceProvider, string key)
//    {
//        // Статические обработчики игнорируются, только динамические
//        return _manager.GetHandlers(key);
//    }
//}
// Updates 26.05.22
public sealed class DynamicKeyRouter: IKeyRouter
{
    // Updates 26.05.22
    private readonly IDynamicSubscriptionManager _manager;

    public DynamicKeyRouter(IDynamicSubscriptionManager manager) => _manager = manager;

    public IEnumerable<Func<IServiceProvider, IMessage, CancellationToken, ValueTask>>
    Resolve(IServiceProvider serviceProvider, string key)
    {
        return _manager.GetHandlers(key);
    }
}