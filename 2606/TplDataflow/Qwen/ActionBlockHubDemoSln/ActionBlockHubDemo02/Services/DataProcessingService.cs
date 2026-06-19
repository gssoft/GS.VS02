// ------------------------------------------
// Services/DataProcessingService.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using ActionBlockHubDemo.Models;
using Microsoft.Extensions.Hosting; // Обязательно IHostedService
using Microsoft.Extensions.Logging;

namespace ActionBlockHubDemo.Services
{
    // Этот класс будет нашим "хэндлером". Он может иметь зависимости, состояние и т.д.
    public class DataHandler
    {
        private readonly string _handlerName;
        private readonly ILogger<DataHandler> _logger;

        public DataHandler(string handlerName, ILogger<DataHandler> logger)
        {
            _handlerName = handlerName;
            _logger = logger;
        }

        public async Task HandleAsync(MyDataType data)
        {
            _logger.LogInformation($"[{_handlerName}] НАЧАЛО обработки: {data}");

            // Имитация тяжелой работы (I/O bound)
            await Task.Delay(500);

            _logger.LogInformation($"[{_handlerName}] КОНЕЦ обработки: {data}");
        }
    }

    // --- ИЗМЕНЕННЫЙ DataProcessingService ---

    public class DataProcessingService : IHostedService
    {
        private readonly ILogger<DataProcessingService> _logger;
        // ИЗМЕНЕНИЕ: Используем интерфейс IBroadcastHub
        private readonly IBroadcastHub<string, MyDataType> _broadcastHub;
        private Timer? _timer;

        public DataProcessingService(ILogger<DataProcessingService> logger, IBroadcastHub<string, MyDataType> broadcastHub)
        {
            _logger = logger;
            _broadcastHub = broadcastHub;
        }

        private int _counter = 0;
        private void SendData(object? state)
        {
            _counter++;

            // Создаем и отправляем сообщения для каждого "канала"
            // ИЗМЕНЕНИЕ: Используем _broadcastHub вместо _actionBlockHub
            var dataA = new MyDataType { Id = _counter, Key = "A", Source = "Генерация" };
            var dataB = new MyDataType { Id = _counter, Key = "B", Source = "Генерация" };
            var dataC = new MyDataType { Id = _counter, Key = "C", Source = "Генерация" };

            // Публикуем через BroadcastHub
            _broadcastHub.PublishAsync("A", dataA);
            _broadcastHub.PublishAsync("B", dataB);
            _broadcastHub.PublishAsync("C", dataC);

            _logger.LogInformation($"Сгенерированы сообщения #{_counter} для A, B и C");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is starting.");
            // Таймер будет "генерировать" данные каждую секунду
            _timer = new Timer(SendData, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is stopping.");
            _timer?.Change(Timeout.Infinite, 0);

            // Теперь мы можем корректно завершить работу и дождаться обработки всех сообщений
            _broadcastHub.Complete();

            // Ждем, пока BroadcastBlock завершится, а благодаря PropagateCompletion, 
            // сигнал уйдет дальше в ActionBlock.
            await _broadcastHub.Completion;

            _logger.LogInformation("Сервис остановлен. Все сообщения обработаны.");
        }

        //public async Task StopAsync(CancellationToken cancellationToken)
        //{
        //    _logger.LogInformation("DataProcessingService is stopping.");
        //    _timer?.Change(Timeout.Infinite, 0);

        //    // ВАЖНОЕ ИЗМЕНЕНИЕ:
        //    // Поскольку мы используем BroadcastHub, нам нужно как-то дать ему сигнал о завершении.
        //    // Простой способ - отправить специальное "последнее" сообщение или просто подождать,
        //    // пока таймер остановится. TPL Dataflow не требует вызова Complete() для BroadcastBlock,
        //    // если мы не хотим блокировать StopAsync ожиданием его завершения.
        //    // Если вы хотите ждать завершения обработки, логика усложняется.

        //    // Для простоты примера просто подождем секунду, чтобы последние сообщения обработались.
        //    await Task.Delay(1500, cancellationToken);

        //    _logger.LogInformation("Сервис остановлен.");
        //}
    }

    //public class DataProcessingService : IHostedService
    //{
    //    private readonly ILogger<DataProcessingService> _logger;
    //    // Добавляем зависимость от BroadcastHub
    //    private readonly BroadcastHub<string, MyDataType> _broadcastHub;
    //    private Timer? _timer; // Таймер для имитации поступления данных

    //    // Внедряем BroadcastHub через конструктор
    //    public DataProcessingService(ILogger<DataProcessingService> logger, BroadcastHub<string, MyDataType> broadcastHub)
    //    {
    //        _logger = logger;
    //        _broadcastHub = broadcastHub;
    //    }

    //    private int _counter = 0;
    //    private void SendData(object? state)
    //    {
    //        _counter++;

    //        // Создаем и отправляем сообщения для каждого "канала"
    //        // ИЗМЕНЕНИЕ: Используем _broadcastHub вместо _actionBlockHub
    //        var dataA = new MyDataType { Id = _counter, Key = "A", Source = "Генерация" };
    //        var dataB = new MyDataType { Id = _counter, Key = "B", Source = "Генерация" };
    //        var dataC = new MyDataType { Id = _counter, Key = "C", Source = "Генерация" };

    //        // Публикуем через BroadcastHub
    //        _broadcastHub.PublishAsync("A", dataA);
    //        _broadcastHub.PublishAsync("B", dataB);
    //        _broadcastHub.PublishAsync("C", dataC);

    //        _logger.LogInformation($"Сгенерированы сообщения #{_counter} для A, B и C");
    //    }

    //    public Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        _logger.LogInformation("DataProcessingService is starting.");
    //        // Таймер будет "генерировать" данные каждую секунду
    //        _timer = new Timer(SendData, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    //        return Task.CompletedTask;
    //    }

    //    public async Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        _logger.LogInformation("DataProcessingService is stopping.");
    //        _timer?.Change(Timeout.Infinite, 0);

    //        // ВАЖНОЕ ИЗМЕНЕНИЕ:
    //        // Поскольку мы используем BroadcastHub, нам нужно как-то дать ему сигнал о завершении.
    //        // Простой способ - отправить специальное "последнее" сообщение или просто подождать,
    //        // пока таймер остановится. TPL Dataflow не требует вызова Complete() для BroadcastBlock,
    //        // если мы не хотим блокировать StopAsync ожиданием его завершения.
    //        // Если вы хотите ждать завершения обработки, логика усложняется.

    //        // Для простоты примера просто подождем секунду, чтобы последние сообщения обработались.
    //        await Task.Delay(1500, cancellationToken);

    //        _logger.LogInformation("Сервис остановлен.");
    //    }
    //}
}

