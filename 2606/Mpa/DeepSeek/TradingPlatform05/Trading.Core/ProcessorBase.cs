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

    protected async Task EnqueueAsync(TMessage message, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(message, ct);

    private async Task ProcessMessagesAsync()
    {
        try
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
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая отмена при остановке
        }
        finally
        {
            Logger.LogDebug("Processor {ProcessorName} stopped processing.", GetType().Name);
        }
    }

    protected abstract Task HandleAsync(TMessage message, CancellationToken ct);

    public virtual async ValueTask DisposeAsync()
    {
        _cts.Cancel();                    // Отменяем токен, чтобы цикл завершился
        _channel.Writer.TryComplete();    // Больше не принимаем новые сообщения
        await _processingTask;            // Дожидаемся завершения обработки
        _cts.Dispose();
    }
}