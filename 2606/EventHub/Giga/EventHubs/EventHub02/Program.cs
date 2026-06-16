// Иммутабельная коллекция (ImmutableDictionary)
// Этот подход основан на неизменяемых коллекциях,
// обеспечивающих автоматическое создание копий при каждом изменении.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public class EventHub
{
    private ImmutableDictionary<string, ImmutableList<Action<string>>> _handlers = ImmutableDictionary.Create<string, ImmutableList<Action<string>>>();

    // Подписывается на указанный тикер
    public void Subscribe(string ticker, Action<string> handler)
    {
        _handlers = _handlers.SetItem(
            ticker,
            _handlers.GetValueOrDefault(ticker)?.Add(handler) ?? ImmutableList.Create(handler));
    }

    // Отписывается от тикера
    public bool Unsubscribe(string ticker, Action<string> handler)
    {
        if (_handlers.TryGetValue(ticker, out var list))
        {
            var updatedList = list.Remove(handler);
            if (updatedList.Count > 0 || !list.Any())
            {
                _handlers = _handlers.SetItem(ticker, updatedList);
                return true;
            }
        }
        return false;
    }

    // Публикует сообщение для указанного тикера
    public void Publish(string ticker, string message)
    {
        if (_handlers.TryGetValue(ticker, out var handlers))
        {
            foreach (var handler in handlers)
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

        hub.Publish("Stocks", "Apple stock is rising.");
        hub.Publish("Weather", "It will rain tomorrow.");
    }
}
