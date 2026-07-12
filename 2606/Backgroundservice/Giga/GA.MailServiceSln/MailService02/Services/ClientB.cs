using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class ClientB : BackgroundService
{
    private readonly ChannelWriter<string> _outgoing;
    private readonly ChannelReader<string> _incoming;
    private int _counter = 0;

    public ClientB(BToHubPipe outPipe, HubToBPipe inPipe)
    {
        _outgoing = outPipe.Writer;
        _incoming = inPipe.Reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1.1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                bool receivedSomething = false;
                while (_incoming.TryRead(out var msg))
                {
                    Console.WriteLine($"[B] Received from A: {msg}");
                    receivedSomething = true;
                }
                if (receivedSomething) Console.WriteLine("[B] --- End of Receive Batch ---");

                var outMsg = $"Alert {_counter++} from B at {DateTime.Now:HH:mm:ss}";

                // WriteAsync тоже может выкинуть исключение, если токен отменится прямо во время записи
                await _outgoing.WriteAsync(outMsg, stoppingToken);
                Console.WriteLine($"[B] Sent: {outMsg}");

                // Вот здесь при CTRL-C вылетает TaskCanceledException
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        // 1. Ловим отмену токена (TaskCanceledException наследуется от OperationCanceledException)
        catch (OperationCanceledException)
        {
            // Это не ошибка, а штатная остановка по CTRL+C
            Console.WriteLine("[B] Graceful shutdown: Cancellation requested.");
        }
        // 2. На всякий случай ловим закрытие канала (если кто-то вызовет Complete() на канале)
        catch (ChannelClosedException)
        {
            Console.WriteLine("[B] Graceful shutdown: Channel was closed.");
        }
        // 3. Ловим все остальные непредвиденные ошибки, чтобы сервис не "упал" молча
        catch (Exception ex)
        {
            Console.WriteLine($"[B] Unexpected error: {ex.Message}");
        }
    }
}


//using System;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Hosting;

//public class ClientB : BackgroundService
//{
//    private readonly ChannelWriter<string> _outgoing;
//    private readonly ChannelReader<string> _incoming;
//    private int _counter = 0;

//    public ClientB(BToHubPipe outPipe, HubToBPipe inPipe)
//    {
//        _outgoing = outPipe.Writer;
//        _incoming = inPipe.Reader;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        await Task.Delay(TimeSpan.FromSeconds(1.1), stoppingToken);

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            bool receivedSomething = false;
//            while (_incoming.TryRead(out var msg))
//            {
//                Console.WriteLine($"[B] Received from A: {msg}");
//                receivedSomething = true;
//            }
//            if (receivedSomething) Console.WriteLine("[B] --- End of Receive Batch ---");

//            var outMsg = $"Alert {_counter++} from B at {DateTime.Now:HH:mm:ss}";
//            await _outgoing.WriteAsync(outMsg, stoppingToken);
//            Console.WriteLine($"[B] Sent: {outMsg}");

//            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);   // Вот здесь делает останов при ctrl-c
//        }
//    }
//}

