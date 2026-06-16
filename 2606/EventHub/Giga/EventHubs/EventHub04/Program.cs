// Вариант №3: Чтение-запись с использованием ReaderWriterLockSlim
// Эта техника применяется, когда требуется разделить операции чтения и записи для повышения производительности.

using System;
using System.Collections.Generic;
using System.Threading;

public class EventHub
{
    private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
    private readonly Dictionary<string, List<Action<string>>> _handlers = new Dictionary<string, List<Action<string>>>();

    // Подписывается на указанный тикер
    public void Subscribe(string ticker, Action<string> handler)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (!_handlers.ContainsKey(ticker))
                _handlers[ticker] = new List<Action<string>>();

            _handlers[ticker].Add(handler);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    // Отписывается от тикера
    public bool Unsubscribe(string ticker, Action<string> handler)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (_handlers.TryGetValue(ticker, out var list))
            {
                list.Remove(handler);
                return true;
            }
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
        return false;
    }

    // Публикует сообщение для указанного тикера
    public void Publish(string ticker, string message)
    {
        _rwLock.EnterReadLock();
        try
        {
            if (_handlers.TryGetValue(ticker, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                    handler.Invoke(message);
            }
        }
        finally
        {
            _rwLock.ExitReadLock();
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

        hub.Publish("Stocks", "Microsoft's stocks are stable.");
        hub.Publish("Weather", "Light breeze expected today.");
    }
}

