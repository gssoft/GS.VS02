
// EventDrivenProcessorBase.cs
using Microsoft.Extensions.Logging;
using Trading.Core;

public abstract class EventDrivenProcessor<TEvent> : ProcessorBase<TEvent>
{
    protected EventDrivenProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, int capacity = 1000)
        : base(bus, loggerFactory, capacity)
    {
        // Подписываемся с правильной сигнатурой: (событие, токен отмены)
        Bus.Subscribe<TEvent>((evt, ct) => EnqueueAsync(evt, ct));
    }
}
