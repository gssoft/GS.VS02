//using System.Threading.Channels;

//public class ClientB : BackgroundService
//{
//    private readonly ChannelWriter<string> _outgoing;
//    private readonly ChannelReader<string> _incoming;
//    private int _counter = 0;

//    public ClientB(
//        [FromKeyedServices("ToB")] ChannelWriter<string> outgoing,   // Читаем из канала Хаба TO_A
//        [FromKeyedServices("FromB")] ChannelReader<string> incoming) // Пишем в свой канал FROM_A (который читает Хаб)
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
//                Console.WriteLine($"[B] Received from A: {msg}");
//                receivedSomething = true;
//            }
//            if (receivedSomething) Console.WriteLine("[B] --- End of Receive Batch ---");

//            // Отправка своего сообщения
//            var outMsg = $"Message {_counter++} from B at {DateTime.Now:HH:mm:ss}";
//            await _outgoing.WriteAsync(outMsg, stoppingToken);
//            Console.WriteLine($"[B] Sent: {outMsg}");

//            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
//        }
//    }
//}



