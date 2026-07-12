using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class ClientA : BackgroundService
{
    private readonly ChannelWriter<string> _outgoing;
    private readonly ChannelReader<string> _incoming;
    private int _counter = 0;

    public ClientA(AToHubPipe outPipe, HubToAPipe inPipe)
    {
        _outgoing = outPipe.Writer;
        _incoming = inPipe.Reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                bool receivedSomething = false;
                while (_incoming.TryRead(out var msg))
                {
                    Console.WriteLine($"[A] Received from B: {msg}");
                    receivedSomething = true;
                }
                if (receivedSomething) Console.WriteLine("[A] --- End of Receive Batch ---");

                var outMsg = $"Message {_counter++} from A at {DateTime.Now:HH:mm:ss}";

                // WriteAsync также может выкинуть исключение при отмене токена или закрытии канала
                await _outgoing.WriteAsync(outMsg, stoppingToken);
                Console.WriteLine($"[A] Sent: {outMsg}");

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        // 1. Штатная остановка по CTRL+C (отмена токена)
        catch (OperationCanceledException)
        {
            Console.WriteLine("[A] Graceful shutdown: Cancellation requested.");
        }
        // 2. Если канал был явно закрыт (например, вызовом Complete())
        catch (ChannelClosedException)
        {
            Console.WriteLine("[A] Graceful shutdown: Channel was closed.");
        }
        // 3. Любые другие непредвиденные ошибки
        catch (Exception ex)
        {
            Console.WriteLine($"[A] Unexpected error: {ex.Message}");
        }
    }
}
