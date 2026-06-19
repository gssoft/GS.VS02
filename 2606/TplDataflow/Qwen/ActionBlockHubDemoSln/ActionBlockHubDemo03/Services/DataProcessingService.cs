// ------------------------------------------
// Services/DataProcessingService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActionBlockHubDemo.Services
{
    // 1. КЛАСС ОБРАБОТЧИКА (Восстановлен)
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

            // ИМИТАЦИЯ СБОЯ ДЛЯ КЛЮЧА "B" (Четные ID) - для теста DLQ
            if (_handlerName == "B" && data.Id % 2 == 0)
            {
                throw new InvalidOperationException($"Искусственная ошибка при обработке ID={data.Id} для ключа B!");
            }

            // Имитация тяжелой работы (I/O bound)
            await Task.Delay(500);

            _logger.LogInformation($"[{_handlerName}] КОНЕЦ обработки: {data}");
        }
    }

    // 2. КЛАСС СЕРВИСА (С поддержкой Options и динамических ключей)
    public class DataProcessingService : IHostedService
    {
        private readonly ILogger<DataProcessingService> _logger;
        private readonly IBroadcastHub<string, MyDataType> _broadcastHub;
        private readonly IActionBlockHub<string, MyDataType> _actionHub;
        private readonly IDeadLetterQueue<MyDataType> _deadLetterQueue;
        private readonly HubOptions _hubOptions;

        // Сохраняем список ключей из конфига
        private readonly List<string> _keys;

        private Timer? _timer;
        private int _counter = 0;
        private volatile bool _isStopping;

        public DataProcessingService(
            ILogger<DataProcessingService> logger,
            IBroadcastHub<string, MyDataType> broadcastHub,
            IActionBlockHub<string, MyDataType> actionHub,
            IDeadLetterQueue<MyDataType> deadLetterQueue,
            IOptions<HubOptions> hubOptions)
        {
            _logger = logger;
            _broadcastHub = broadcastHub;
            _actionHub = actionHub;
            _deadLetterQueue = deadLetterQueue;
            _hubOptions = hubOptions.Value;

            // Запоминаем ключи из конфигурации
            _keys = _hubOptions.ActionBlock.Keys;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is starting.");
            // Используем интервал из конфига
            _timer = new Timer(SendData, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_hubOptions.Generator.IntervalMs));
            return Task.CompletedTask;
        }

        private async void SendData(object? state)
        {
            if (_isStopping) return;

            try
            {
                _counter++;
                var tasks = new List<Task>();

                // Динамически генерируем сообщения для ВСЕХ ключей из конфига
                foreach (var key in _keys)
                {
                    var data = new MyDataType { Id = _counter, Key = key, Source = "Генерация" };
                    tasks.Add(_broadcastHub.PublishAsync(key, data));
                }

                await Task.WhenAll(tasks);
                _logger.LogInformation($"Сгенерированы сообщения #{_counter} для [{string.Join(", ", _keys)}]");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка при отправке данных в таймере");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataProcessingService is stopping.");
            _isStopping = true;
            _timer?.Change(Timeout.Infinite, 0);

            _broadcastHub.Complete();
            await _broadcastHub.Completion;

            _logger.LogInformation("=== ИТОГОВАЯ СТАТИСТИКА ЗА СЕАНС ==================");
            // Выводим статистику по всем ключам из конфига
            foreach (var key in _keys)
            {
                _logger.LogInformation("🏁 [{Key}]: Всего обработано = {Count}, Ошибок = {Errors}",
                    key, _actionHub.GetProcessedCount(key), _actionHub.GetErrorCount(key));
            }

            var deadLetters = _deadLetterQueue.GetAll();
            if (deadLetters.Any())
            {
                _logger.LogWarning("⚠️ ВНИМАНИЕ! Найдено {Count} недоставленных сообщений (Dead Letters):", deadLetters.Count);
                foreach (var dl in deadLetters)
                {
                    _logger.LogWarning("💀 {DeadLetterInfo}", dl.ToString());
                }
            }
            else
            {
                _logger.LogInformation("✅ Dead Letter Queue пуста. Все сообщения обработаны успешно!");
            }
            _logger.LogInformation("====================================================");

            _logger.LogInformation("Сервис остановлен. Все сообщения обработаны.");
        }
    }
}

//using ActionBlockHubDemo.Models;
//using ActionBlockHubDemo.Options;
//using ActionBlockHubDemo.Services;
//using Microsoft.Extensions.Options;
//// ... другие using

//public class DataProcessingService : IHostedService
//{
//    private readonly ILogger<DataProcessingService> _logger;
//    private readonly IBroadcastHub<string, MyDataType> _broadcastHub;
//    private readonly IActionBlockHub<string, MyDataType> _actionHub;
//    private readonly IDeadLetterQueue<MyDataType> _deadLetterQueue;
//    private readonly HubOptions _hubOptions;

//    // ❗ НОВОЕ: Сохраняем список ключей из конфига
//    private readonly List<string> _keys;

//    private Timer? _timer;
//    private int _counter = 0;
//    private volatile bool _isStopping;

//    public DataProcessingService(
//        ILogger<DataProcessingService> logger,
//        IBroadcastHub<string, MyDataType> broadcastHub,
//        IActionBlockHub<string, MyDataType> actionHub,
//        IDeadLetterQueue<MyDataType> deadLetterQueue,
//        IOptions<HubOptions> hubOptions)
//    {
//        _logger = logger;
//        _broadcastHub = broadcastHub;
//        _actionHub = actionHub;
//        _deadLetterQueue = deadLetterQueue;
//        _hubOptions = hubOptions.Value;

//        // ❗ НОВОЕ: Запоминаем ключи
//        _keys = _hubOptions.ActionBlock.Keys;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("DataProcessingService is starting.");
//        _timer = new Timer(SendData, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_hubOptions.Generator.IntervalMs));
//        return Task.CompletedTask;
//    }

//    private int _counterLocal = 0;
//    private async void SendData(object? state)
//    {
//        if (_isStopping) return;

//        try
//        {
//            _counterLocal++;
//            var tasks = new List<Task>();

//            // ❗ НОВОЕ: Динамически генерируем сообщения для ВСЕХ ключей из конфига
//            foreach (var key in _keys)
//            {
//                var data = new MyDataType { Id = _counterLocal, Key = key, Source = "Генерация" };
//                tasks.Add(_broadcastHub.PublishAsync(key, data));
//            }

//            await Task.WhenAll(tasks);
//            _logger.LogInformation($"Сгенерированы сообщения #{_counterLocal} для [{string.Join(", ", _keys)}]");
//        }
//        catch (Exception ex) when (ex is not OperationCanceledException)
//        {
//            _logger.LogError(ex, "Ошибка при отправке данных в таймере");
//        }
//    }

//    public async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("DataProcessingService is stopping.");
//        _isStopping = true;
//        _timer?.Change(Timeout.Infinite, 0);

//        _broadcastHub.Complete();
//        await _broadcastHub.Completion;

//        _logger.LogInformation("=== ИТОГОВАЯ СТАТИСТИКА ЗА СЕАНС ==================");
//        // ❗ НОВОЕ: Выводим статистику по всем ключам из конфига
//        foreach (var key in _keys)
//        {
//            _logger.LogInformation("🏁 [{Key}]: Всего обработано = {Count}, Ошибок = {Errors}",
//                key, _actionHub.GetProcessedCount(key), _actionHub.GetErrorCount(key));
//        }

//        var deadLetters = _deadLetterQueue.GetAll();
//        if (deadLetters.Any())
//        {
//            _logger.LogWarning("⚠️ ВНИМАНИЕ! Найдено {Count} недоставленных сообщений (Dead Letters):", deadLetters.Count);
//            foreach (var dl in deadLetters)
//            {
//                _logger.LogWarning("💀 {DeadLetterInfo}", dl.ToString());
//            }
//        }
//        else
//        {
//            _logger.LogInformation("✅ Dead Letter Queue пуста. Все сообщения обработаны успешно!");
//        }
//        _logger.LogInformation("====================================================");

//        _logger.LogInformation("Сервис остановлен. Все сообщения обработаны.");
//    }
//}

//using ActionBlockHubDemo.Models;
//using ActionBlockHubDemo.Options;
//using Microsoft.Extensions.Hosting; // Обязательно IHostedService
//using Microsoft.Extensions.Logging;
//using System;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ActionBlockHubDemo.Services;
////{
//    // Этот класс будет нашим "хэндлером". Он может иметь зависимости, состояние и т.д.
//    public class DataHandler
//    {
//        private readonly string _handlerName;
//        private readonly ILogger<DataHandler> _logger;


//        public DataHandler(string handlerName, ILogger<DataHandler> logger)
//        {
//            _handlerName = handlerName;
//            _logger = logger;
//        }

//        public async Task HandleAsync(MyDataType data)
//        {
//            _logger.LogInformation($"[{_handlerName}] НАЧАЛО обработки: {data}");

//            // ИМИТАЦИЯ СБОЯ ДЛЯ КЛЮЧА "B" (Четные ID)
//            if (_handlerName == "B" && data.Id % 2 == 0)
//            {
//                throw new InvalidOperationException($"Искусственная ошибка при обработке ID={data.Id} для ключа B!");
//            }

//            // Имитация тяжелой работы (I/O bound)
//            await Task.Delay(500);
//            _logger.LogInformation($"[{_handlerName}] КОНЕЦ обработки: {data}");
//        }
//    }

//    // --- ИЗМЕНЕННЫЙ DataProcessingService ---

//    public class DataProcessingService : IHostedService
//    {
//    //private readonly ILogger<DataProcessingService> _logger;
//    //// ИЗМЕНЕНИЕ: Используем интерфейс IBroadcastHub
//    //private readonly IBroadcastHub<string, MyDataType> _broadcastHub;
//    //private readonly IActionBlockHub<string, MyDataType> _actionHub;
//    //private readonly IDeadLetterQueue<MyDataType> _deadLetterQueue;

//    private readonly ILogger<DataProcessingService> _logger;
//    private readonly IBroadcastHub<string, MyDataType> _broadcastHub;
//    private readonly IActionBlockHub<string, MyDataType> _actionHub;
//    private readonly IDeadLetterQueue<MyDataType> _deadLetterQueue;
//    private readonly HubOptions _hubOptions; // <-- Новое
//    private Timer? _timer;
//    private int _counter = 0;
//    private volatile bool _isStopping;

//    public DataProcessingService(ILogger<DataProcessingService> logger,
//        IBroadcastHub<string, MyDataType> broadcastHub,
//        IActionBlockHub<string, MyDataType> actionHub,
//        IDeadLetterQueue<MyDataType> deadLetterQueue)
//    {
//        _logger = logger;
//        _broadcastHub = broadcastHub;
//        _actionHub = actionHub;
//        _deadLetterQueue = deadLetterQueue;
//    }

//    private async void SendData(object? state)
//    {
//        if (_isStopping) return; // Если начали останавливаться — ничего не генерируем

//        try
//        {
//            _counter++;
//            var dataA = new MyDataType { Id = _counter, Key = "A", Source = "Генерация" };
//            var dataB = new MyDataType { Id = _counter, Key = "B", Source = "Генерация" };
//            var dataC = new MyDataType { Id = _counter, Key = "C", Source = "Генерация" };

//            // Ждем, пока все три сообщения примутся в BroadcastHub
//            // (если там переполнится очередь, SendAsync притормозит нас)
//            await Task.WhenAll(
//                _broadcastHub.PublishAsync("A", dataA),
//                _broadcastHub.PublishAsync("B", dataB),
//                _broadcastHub.PublishAsync("C", dataC)
//            );

//            _logger.LogInformation($"Сгенерированы сообщения #{_counter} для A, B и C");
//        }
//        catch (Exception ex) when (ex is not OperationCanceledException)
//        {
//            // В async void try-catch ОБЯЗАТЕЛЕН, иначе процесс упадет
//            _logger.LogError(ex, "Ошибка при отправке данных в таймере");
//        }
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
//        _isStopping = true; // Поднимаем флаг, чтобы таймер больше ничего не слал
//        _timer?.Change(Timeout.Infinite, 0);

//        // ЗАВЕРШАЕМ ИМЕННО BROADCASTHUB, так как это начало цепочки!
//        _broadcastHub.Complete();

//        // Ждем, пока сигнал Complete() пройдет по всей цепочке (благодаря PropagateCompletion)
//        await _broadcastHub.Completion;

//        // ... вывод итоговой статистики ...
//        // --- ИТОГОВЫЙ ОТЧЕТ ---
//        _logger.LogInformation("=== ИТОГОВАЯ СТАТИСТИКА ЗА СЕАНС ==================");
//        foreach (var key in new[] { "A", "B", "C" })
//        {
//            _logger.LogInformation("🏁 [{Key}]: Всего обработано = {Count}, Ошибок = {Errors}",
//                key, _actionHub.GetProcessedCount(key), _actionHub.GetErrorCount(key));
//        }

//        // --- ВЫВОД DEAD LETTER QUEUE ---
//        var deadLetters = _deadLetterQueue.GetAll();
//        if (deadLetters.Any())
//        {
//            _logger.LogWarning("⚠️ ВНИМАНИЕ! Найдено {Count} недоставленных сообщений (Dead Letters):", deadLetters.Count);
//            foreach (var dl in deadLetters)
//            {
//                _logger.LogWarning("💀 {DeadLetterInfo}", dl.ToString());
//            }
//        }
//        else
//        {
//            _logger.LogInformation("✅ Dead Letter Queue пуста. Все сообщения обработаны успешно!");
//        }

//        _logger.LogInformation("====================================================");

//        _logger.LogInformation("Сервис остановлен. Все сообщения обработаны.");
//    }
//}   
// }

