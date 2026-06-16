using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub01
{
    // Основной класс EventHub
    public class EventHub : IPublisherService, ISubscriberService
    {
        // Хранение тикеров и соответствующих обработчиков
        private readonly Dictionary<string, List<Action<string>>> _handlers =
            new Dictionary<string, List<Action<string>>>();

        // Добавление нового обработчика для конкретного тикера
        public void AddHandler(string ticker, Action<string> handler)
        {
            lock (_handlers)
            {
                if (!_handlers.ContainsKey(ticker))
                    _handlers.Add(ticker, new List<Action<string>>());

                _handlers[ticker].Add(handler);
            }
        }

        // Выполнение всех зарегистрированных обработчиков для указанного тикера
        public void InvokeHandlers(string ticker, string message)
        {
            lock (_handlers)
            {
                if (_handlers.TryGetValue(ticker, out var handlers))
                {
                    foreach (var handler in handlers.ToList())
                        handler.Invoke(message);
                }
            }
        }

        // Реализация методов интерфейсов
        public void Publish(string ticker, string message)
        {
            InvokeHandlers(ticker, message);
        }

        public void Subscribe(string ticker, Action<string> handler)
        {
            AddHandler(ticker, handler);
        }
    }
}
