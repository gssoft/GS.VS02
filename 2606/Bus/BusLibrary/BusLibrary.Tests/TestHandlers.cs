
using BusLibrary;
using System.Collections.Concurrent;

namespace BusLibrary.Tests;

[Handles("test:message")]
public class StaticTestHandler : IMessageHandler<TestMessage>
{
    public static ConcurrentBag<TestMessage> ReceivedMessages { get; } = new();

    public ValueTask Handle(TestMessage message, CancellationToken ct)
    {
        ReceivedMessages.Add(message);
        return ValueTask.CompletedTask;
    }
}

[Handles("another:message")]
public class AnotherStaticHandler : IMessageHandler<AnotherTestMessage>
{
    public static ConcurrentBag<AnotherTestMessage> ReceivedMessages { get; } = new();

    public ValueTask Handle(AnotherTestMessage message, CancellationToken ct)
    {
        ReceivedMessages.Add(message);
        return ValueTask.CompletedTask;
    }
}

// Для динамических тестов — обработчик без атрибута
public class DynamicTestHandler : IMessageHandler<TestMessage>
{
    public ConcurrentBag<TestMessage> Received { get; } = new();
    public ValueTask Handle(TestMessage message, CancellationToken ct)
    {
        Received.Add(message);
        return ValueTask.CompletedTask;
    }
}
