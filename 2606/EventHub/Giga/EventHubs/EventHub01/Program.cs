using EventHub01;

class Program
{
    static void Main()
    {
        // Создание экземпляра EventHub
        var hub = new EventHub();

        // Регистрация слушателя для тикера "NewsTicker"
        hub.Subscribe("NewsTicker", msg => Console.WriteLine($"Received news: {msg}"));

        // Публикация сообщения
        hub.Publish("NewsTicker", "Hello from the publisher");
    }
}
