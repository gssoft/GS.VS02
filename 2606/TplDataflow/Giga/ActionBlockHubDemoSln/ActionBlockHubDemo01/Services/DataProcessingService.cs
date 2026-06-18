// Services/DataProcessingService.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActionBlockHubDemo.Models;
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

    public class DataProcessingService : IHostedService
    {
        private readonly ILogger<DataProcessingService> _logger;
        private readonly ActionBlockHub<string, MyDataType> _actionBlockHub;
        private Timer? _timer; // Таймер для имитации поступления данных

        public DataProcessingService(ILogger<DataProcessingService> logger,
                                     ActionBlockHub<string, MyDataType> actionBlockHub)
        {
            _logger = logger;
            _actionBlockHub = actionBlockHub;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is starting.");

            // Таймер будет "генерировать" данные каждую секунду
            _timer = new Timer(SendData, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        private int _counter = 0;
        private void SendData(object? state)
        {
            _counter++;

            // Создаем и отправляем сообщения для каждого "канала"
            var dataA = new MyDataType { Id = _counter, Key = "A", Source = "Генерация" };
            var dataB = new MyDataType { Id = _counter, Key = "B", Source = "Генерация" };
            var dataC = new MyDataType { Id = _counter, Key = "C", Source = "Генерация" };

            _actionBlockHub.PublishAsync("A", dataA);
            _actionBlockHub.PublishAsync("B", dataB);
            _actionBlockHub.PublishAsync("C", dataC);

            _logger.LogInformation($"Сгенерированы сообщения #{_counter} для A, B и C");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is stopping.");

            // 1. Остановка генерации новых данных
            _timer?.Change(Timeout.Infinite, 0);

            // 2. Сигнал блокам о завершении приема сообщений
            _actionBlockHub.Complete();

            // 3. Ожидание завершения обработки всех оставшихся в очередях сообщений
            await _actionBlockHub.Completion;

            _logger.LogInformation("Все сообщения обработаны. Сервис остановлен.");
        }
    }
}

