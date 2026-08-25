using System;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPlatform.Abstractions;

public interface IMicroEventBus : IDisposable
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}

