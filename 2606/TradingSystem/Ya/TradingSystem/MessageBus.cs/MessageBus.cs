/*
## 🎯 PROJECT 2: TradingSystem.Core.MessageBus
**Тип * *: Class Library
**Назначение**: Универсальный транспорт для всех типов взаимодействия
*/
// ### Интерфейсы:

using System.Collections.Concurrent;
using System.Threading.Channels;

public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default);
}

public interface IMessageBusFactory
{
    IMessageBus CreateInProcess();           // Channels
    IMessageBus CreateNamedPipes(string name); // Named Pipes
    IMessageBus CreateTcp(string host, int port); // TCP
}
/*
### Реализации:

```csharp
*/

// 1. In-Process (Channels)
public class ChannelMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<object>> _channels;
}

// 2. Named Pipes (IPC)
public class NamedPipeMessageBus : IMessageBus
{
    private readonly string _pipeName;
}

// 3. TCP (Network)
public class TcpMessageBus : IMessageBus
{
    private readonly string _host;
    private readonly int _port;
}


