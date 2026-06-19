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
        private readonly IActionBlockHub<string, MyDataType> _actionHub;

        private Timer? _timer;

        public DataProcessingService(ILogger<DataProcessingService> logger,
            IBroadcastHub<string, MyDataType> broadcastHub,
            IActionBlockHub<string, MyDataType> actionHub)
        {
            _logger = logger;
            _broadcastHub = broadcastHub;
            _actionHub = actionHub;
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

            _broadcastHub.Complete();
            await _broadcastHub.Completion; // Ждем, пока все очереди опустеют

            // --- ИТОГОВЫЙ ОТЧЕТ ---
            _logger.LogInformation("=== ИТОГОВАЯ СТАТИСТИКА ЗА СЕАНС ==================");
            foreach (var key in new[] { "A", "B", "C" })
            {
                _logger.LogInformation("🏁 [{Key}]: Всего обработано = {Count}, Ошибок = {Errors}",
                    key, _actionHub.GetProcessedCount(key), _actionHub.GetErrorCount(key));
            }
            _logger.LogInformation("====================================================");

            _logger.LogInformation("Сервис остановлен. Все сообщения обработаны.");
        }
    }   
}

