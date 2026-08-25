// ProcessorBase.cs
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Trading.Core;

public abstract class ProcessorBase<TMessage> : IAsyncDisposable
{
    private readonly Channel<TMessage> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    protected readonly IMicroEventBus Bus;
    protected readonly ILogger Logger;

    protected ProcessorBase(IMicroEventBus bus, ILoggerFactory loggerFactory, int capacity = 1000)
    {
        Bus = bus;
        Logger = loggerFactory.CreateLogger(GetType());
        _channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _processingTask = Task.Run(ProcessMessagesAsync);
    }

    /// <summary>Отправить сообщение во внутреннюю очередь процессора.</summary>
    protected async Task EnqueueAsync(TMessage message, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(message, ct);

    /// <summary>Главный цикл обработки – выполняется в изолированном контексте.</summary>
    private async Task ProcessMessagesAsync()
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                await HandleAsync(message, _cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling message {MessageType}", typeof(TMessage).Name);
                // Здесь можно добавить политику повторных попыток или dead-letter
            }
        }
    }

    /// <summary>Переопределяется в конкретном процессоре – содержит бизнес‑логику.</summary>
    protected abstract Task HandleAsync(TMessage message, CancellationToken ct);

    // Подписка на события шины: в конструкторе наследника вызываем Bus.Subscribe<T>(msg => EnqueueAsync(msg))
    // Можно автоматизировать через рефлексию или генератор.

    public virtual async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _processingTask;
        _cts.Cancel();
        _cts.Dispose();
    }
}
