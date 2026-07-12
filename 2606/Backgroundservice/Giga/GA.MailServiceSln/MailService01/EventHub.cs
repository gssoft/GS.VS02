//using System.Threading.Channels;

//public class EventHub : BackgroundService
//{
//    private readonly ChannelReader<string> _fromA;
//    private readonly ChannelWriter<string> _toA;
//    private readonly ChannelReader<string> _fromB;
//    private readonly ChannelWriter<string> _toB;

//    public EventHub(
//        // Указываем контейнеру, какой именно Reader нам нужен по имени ключа
//        [FromKeyedServices("FromA")] ChannelReader<string> fromA,
//        [FromKeyedServices("ToA")] ChannelWriter<string> toA,

//        [FromKeyedServices("FromB")] ChannelReader<string> fromB,
//        [FromKeyedServices("ToB")] ChannelWriter<string> toB)
//    {
//        (_fromA, _toA, _fromB, _toB) = (fromA, toA, fromB, toB);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            if (_fromA.TryRead(out var msgA))
//            {
//                Console.WriteLine($"[HUB] Relay A -> B: {msgA}");
//                await _toB.WriteAsync(msgA, stoppingToken);
//            }

//            if (_fromB.TryRead(out var msgB))
//            {
//                Console.WriteLine($"[HUB] Relay B -> A: {msgB}");
//                await _toA.WriteAsync(msgB, stoppingToken);
//            }

//            await Task.Delay(10, stoppingToken); // Пауза для CPU
//        }
//    }
//}

//using System.Threading.Channels;

//public class EventHub : BackgroundService
//{
//    private readonly ChannelReader<string> _fromA;
//    private readonly ChannelWriter<string> _toA;
//    private readonly ChannelReader<string> _fromB;
//    private readonly ChannelWriter<string> _toB;

//    public EventHub(
//        // Теперь четко сказано: "Дай мне ЧИТАТЕЛЬ писем ОТ клиента А"
//        [FromKeyedServices("FromA")] ChannelReader<string> fromA,
//        [FromKeyedServices("ToA")] ChannelWriter<string> toA,

//        [FromKeyedServices("FromB")] ChannelReader<string> fromB,
//        [FromKeyedServices("ToB")] ChannelWriter<string> toB)
//    {
//        (_fromA, _toA, _fromB, _toB) = (fromA, toA, fromB, toB);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            // Читаем от А БЕЗ ожиданий
//            if (_fromA.TryRead(out var msgA))
//            {
//                Console.WriteLine($"[HUB] Relay A -> B: {msgA}");
//                await _toB.WriteAsync(msgA, stoppingToken);
//            }

//            // Читаем от Б БЕЗ ожиданий
//            if (_fromB.TryRead(out var msgB))
//            {
//                Console.WriteLine($"[HUB] Relay B -> A: {msgB}");
//                await _toA.WriteAsync(msgB, stoppingToken);
//            }

//            // Пауза критически важна здесь, иначе CPU улетит в 100%
//            await Task.Delay(10, stoppingToken);
//        }
//    }

//protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//{
//    while (!stoppingToken.IsCancellationRequested)
//    {
//        //Task.Delay(1000).Wait();
//        //Console.WriteLine($"[HUB] LIVING");

//        // Обработка очереди от Клиента А
//        if (await _fromA.WaitToReadAsync(stoppingToken))
//        {
//            Console.WriteLine($"[AAA] LIVING");

//            while (_fromA.TryRead(out var messageFromA))
//            {
//                Console.WriteLine($"[HUB] Relay A -> B: {messageFromA}");
//                await _toB.WriteAsync(messageFromA, stoppingToken);
//            }
//        }

//        // Обработка очереди от Клиента Б
//        if (await _fromB.WaitToReadAsync(stoppingToken))
//        {
//            Console.WriteLine($"[BBB] LIVING");

//            while (_fromB.TryRead(out var messageFromB))
//            {
//                Console.WriteLine($"[HUB] Relay B -> A: {messageFromB}");
//                await _toA.WriteAsync(messageFromB, stoppingToken);
//            }
//        }

//        // Небольшая пауза, чтобы хаб не крутился впустую на 100% ядра, 
//        // если обе очереди пусты (опционально, можно убрать).
//        await Task.Delay(50, stoppingToken);
//    }
//}
// }

