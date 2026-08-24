using MicroPlatform.Core;
using MicroPlatform.Core.Events;
using Xunit;

namespace MicroPlatform.Core.Tests;

public class InMemoryMicroEventBusTests
{
    [Fact]
    public async Task PublishAsync_CallsSubscribedHandler()
    {
        var bus = new InMemoryMicroEventBus();
        OrderPaid? received = null;
        bus.Subscribe<OrderPaid>((e, _) =>
        {
            received = e;
            return Task.CompletedTask;
        });

        var published = new OrderPaid(Guid.NewGuid(), 100m);
        await bus.PublishAsync(published);

        Assert.Equal(published, received);
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new InMemoryMicroEventBus();

        var exception = await Record.ExceptionAsync(
            () => bus.PublishAsync(new OrderPaid(Guid.NewGuid(), 1m)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WhenOneHandlerThrows_OtherHandlerStillRuns()
    {
        var bus = new InMemoryMicroEventBus();
        var secondHandlerCalled = false;

        bus.Subscribe<OrderPaid>((_, _) => throw new InvalidOperationException("boom"));
        bus.Subscribe<OrderPaid>((_, _) =>
        {
            secondHandlerCalled = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<AggregateException>(
            () => bus.PublishAsync(new OrderPaid(Guid.NewGuid(), 1m)));

        Assert.True(secondHandlerCalled);
    }

    [Fact]
    public async Task Unsubscribe_WithSameDelegateInstance_StopsHandlerFromBeingCalled()
    {
        var bus = new InMemoryMicroEventBus();
        var callCount = 0;

        Task Handler(OrderPaid _, CancellationToken __)
        {
            callCount++;
            return Task.CompletedTask;
        }

        // Same method group / delegate reference is used for both calls -
        // this is the pattern that makes Delegate equality actually work.
        bus.Subscribe<OrderPaid>(Handler);
        bus.Unsubscribe<OrderPaid>(Handler);

        await bus.PublishAsync(new OrderPaid(Guid.NewGuid(), 1m));

        Assert.Equal(0, callCount);
    }
}
