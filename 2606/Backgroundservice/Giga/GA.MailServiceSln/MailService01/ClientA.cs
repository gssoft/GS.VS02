//using System.Threading.Channels;

//public class ClientA : BackgroundService
//{
//    private readonly ChannelWriter<string> _outgoing;
//    private readonly ChannelReader<string> _incoming;
//    private int _counter = 0;

//    public ClientA(
//        [FromKeyedServices("ToA")] ChannelWriter<string> outgoing,   // Читаем из канала Хаба TO_A
//        [FromKeyedServices("FromA")] ChannelReader<string> incoming) // Пишем в свой канал FROM_A (который читает Хаб)
//    {
//        _outgoing = outgoing;
//        _incoming = incoming;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        // ВАЖНО: Сначала ждем секунду, чтобы Hub успел стартовать
//        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            // Чтение ответов
//            bool receivedSomething = false;
//            while (_incoming.TryRead(out var msg))
//            {
//                Console.WriteLine($"[A] Received from B: {msg}");
//                receivedSomething = true;
//            }
//            if (receivedSomething) Console.WriteLine("[A] --- End of Receive Batch ---");

//            // Отправка своего сообщения
//            var outMsg = $"Message {_counter++} from A at {DateTime.Now:HH:mm:ss}";
//            await _outgoing.WriteAsync(outMsg, stoppingToken);
//            Console.WriteLine($"[A] Sent: {outMsg}");

//            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
//        }
//    }
//}

//using System.Threading.Channels;

//public class ClientA : BackgroundService
//{
//    private readonly ChannelWriter<string> _outgoing; // В Hub
//    private readonly ChannelReader<string> _incoming; // Из Hub
//    private int _counter = 0;

//    public ClientA(ChannelWriter<string> outgoing, ChannelReader<string> incoming)
//    {
//        (_outgoing, _incoming) = (outgoing, incoming);

//        // Даем EventHub время "поднять" свои каналы перед первым тиком клиента
//        Task.Delay(500).Wait();
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        // Ждем 1 секунду ПЕРЕД первой отправкой
//        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            // Фаза 1: Сначала читаем ВСЕ накопившиеся ответы
//            bool receivedSomething = false;
//            while (_incoming.TryRead(out var msg))
//            {
//                Console.WriteLine($"[A] Received from B: {msg}");
//                receivedSomething = true;
//            }

//            if (receivedSomething)
//            {
//                Console.WriteLine("[A] --- End of Receive Batch ---");
//            }

//            // Фаза 2: Генерируем новое исходящее событие
//            var outMsg = $"Message {_counter++} from A at {DateTime.Now:HH:mm:ss}";
//            await _outgoing.WriteAsync(outMsg, stoppingToken);
//            Console.WriteLine($"[A] Sent: {outMsg}");

//            // Фаза 3: Спим до следующего цикла
//            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
//        }
//    }
//}
