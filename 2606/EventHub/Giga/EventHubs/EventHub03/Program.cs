// Вариант №2: Thread-Safe Коллекция(ConcurrentDictionary)
// Здесь используется специализированная коллекция, предназначенная для работы в многопоточных средах,
// что устраняет необходимость вручную ставить блокировки.



using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class EventHub
{
    private ConcurrentDictionary<string, List<Action<string>>> _handlers = 
        new ConcurrentDictionary<string, List<Action<string>>>();

    // Подписывается на указанный тикер
    public void Subscribe(string ticker, Action<string> handler)
    {
        _handlers.AddOrUpdate(ticker, key => new List<Action<string>> { handler },
                              (key, oldVal) =>
                              {
                                  oldVal.Add(handler);
                                  return oldVal;
                              });
    }

    // Отписывается от тикера
    public bool Unsubscribe(string ticker, Action<string> handler)
    {
        if (_handlers.TryGetValue(ticker, out var list))
        {
            list.Remove(handler);
            return true;
        }
        return false;
    }

    // Публикует сообщение для указанного тикера
    public void Publish(string ticker, string message)
    {
        if (_handlers.TryGetValue(ticker, out var handlers))
        {
            foreach (var handler in handlers.ToList())
                handler.Invoke(message);
        }
    }
}

class Program
{
    static void Main()
    {
        var hub = new EventHub();
        hub.Subscribe("Stocks", msg => Console.WriteLine($"Stock update: {msg}"));
        hub.Subscribe("Weather", msg => Console.WriteLine($"Weather report: {msg}"));

        hub.Publish("Stocks", "Google stock dropped.");
        hub.Publish("Weather", "Sunny day ahead.");
    }
}
