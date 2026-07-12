using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

// ==========================================
// 1. ОБЕРТКИ ДЛЯ КАНАЛОВ (Уникальные типы вместо интерфейсов)
// Это решает проблему "как отличить два ChannelWriter<string>"
// ==========================================
public sealed class AToHubPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;
    public AToHubPipe(ChannelWriter<string> w, ChannelReader<string> r) { Writer = w; Reader = r; }
}

public sealed class HubToAPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;
    public HubToAPipe(ChannelWriter<string> w, ChannelReader<string> r) { Writer = w; Reader = r; }
}

public sealed class BToHubPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;
    public BToHubPipe(ChannelWriter<string> w, ChannelReader<string> r) { Writer = w; Reader = r; }
}

public sealed class HubToBPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;
    public HubToBPipe(ChannelWriter<string> w, ChannelReader<string> r) { Writer = w; Reader = r; }
}


// ==========================================
// 2. КЛАССЫ СЕРВИСОВ (EventHub, ClientA, ClientB)
// ==========================================

public class EventHub : BackgroundService
{
    private readonly ChannelReader<string> _fromA;
    private readonly ChannelWriter<string> _toA;
    private readonly ChannelReader<string> _fromB;
    private readonly ChannelWriter<string> _toB;

    // Внедряем наши уникальные обертки
    public EventHub(AToHubPipe aIn, HubToAPipe aOut, BToHubPipe bIn, HubToBPipe bOut)
    {
        _fromA = aIn.Reader;
        _toA = aOut.Writer;
        _fromB = bIn.Reader;
        _toB = bOut.Writer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_fromA.TryRead(out var msgA))
            {
                Console.WriteLine($"[HUB] Relay A -> B: {msgA}");
                await _toB.WriteAsync(msgA, stoppingToken);
            }
            if (_fromB.TryRead(out var msgB))
            {
                Console.WriteLine($"[HUB] Relay B -> A: {msgB}");
                await _toA.WriteAsync(msgB, stoppingToken);
            }
            await Task.Delay(10, stoppingToken);
        }
    }
}

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
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool receivedSomething = false;
            while (_incoming.TryRead(out var msg)) { Console.WriteLine($"[A] Received from B: {msg}"); receivedSomething = true; }
            if (receivedSomething) Console.WriteLine("[A] --- End of Receive Batch ---");

            var outMsg = $"Message {_counter++} from A at {DateTime.Now:HH:mm:ss}";
            await _outgoing.WriteAsync(outMsg, stoppingToken);
            Console.WriteLine($"[A] Sent: {outMsg}");

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

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
        await Task.Delay(TimeSpan.FromSeconds(1.1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool receivedSomething = false;
            while (_incoming.TryRead(out var msg)) { Console.WriteLine($"[B] Received from A: {msg}"); receivedSomething = true; }
            if (receivedSomething) Console.WriteLine("[B] --- End of Receive Batch ---");

            var outMsg = $"Alert {_counter++} from B at {DateTime.Now:HH:mm:ss}";
            await _outgoing.WriteAsync(outMsg, stoppingToken);
            Console.WriteLine($"[B] Sent: {outMsg}");

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

// ==========================================
// 3. ТОЧКА ВХОДА (MAIN) - Настройка контейнера
// ==========================================
internal static class Program
{
    private static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Создаем физические каналы
                var aToHubChan = Channel.CreateUnbounded<string>();
                var hubToAChan = Channel.CreateUnbounded<string>();
                var bToHubChan = Channel.CreateUnbounded<string>();
                var hubToBChan = Channel.CreateUnbounded<string>();

                // РЕГИСТРАЦИЯ УНИКАЛЬНЫХ ТИПОВ (Больше нет конфликта!)
                services.AddSingleton(new AToHubPipe(aToHubChan.Writer, aToHubChan.Reader));
                services.AddSingleton(new HubToAPipe(hubToAChan.Writer, hubToAChan.Reader));

                services.AddSingleton(new BToHubPipe(bToHubChan.Writer, bToHubChan.Reader));
                services.AddSingleton(new HubToBPipe(hubToBChan.Writer, hubToBChan.Reader));

                // Регистрируем воркеры
                services.AddHostedService<EventHub>();
                services.AddHostedService<ClientA>();
                services.AddHostedService<ClientB>();
            })
            .Build();

        await host.RunAsync();
    }
}


