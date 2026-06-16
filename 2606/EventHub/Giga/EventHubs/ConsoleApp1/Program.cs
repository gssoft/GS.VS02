using System;

// Класс с событием
public class MyClassWithEvent
{
    // Объявляем делегат для нашего события
    public delegate void SimpleEventHandler(string message);

    // Создаем событие на основе делегата
    public event SimpleEventHandler? OnSimpleEvent;

    // Метод, инициирующий событие
    public void RaiseEvent()
    {
        if (OnSimpleEvent != null)
            OnSimpleEvent("Событие произошло!");
    }
}

class Program
{
    static void Main()
    {
        var myInstance = new MyClassWithEvent();

        // Подписываемся на событие
        myInstance.OnSimpleEvent += HandleSimpleEvent;

        // Вызываем метод, который поднимает событие
        myInstance.RaiseEvent();
    }

    // Метод-обработчик события
    private static void HandleSimpleEvent(string message)
    {
        Console.WriteLine(message); // Выведет сообщение, переданное в обработчике
    }
}
