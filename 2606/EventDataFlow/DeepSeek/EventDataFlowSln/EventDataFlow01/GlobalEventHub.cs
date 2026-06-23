// GlobalEventHub.cs
using System.Collections.Concurrent;
using System.Threading.Channels;

public class GlobalEventHub
{
    // Словарь: Ключ - ID сервиса-приемника, Значение - его входящий канал
    private readonly ConcurrentDictionary<string, Channel<IApplicationEvent>> _serviceChannels = new();

    // Регистрирует сервис-приемник и его канал в глобальном хабе
    public void RegisterServiceChannel(string serviceId, Channel<IApplicationEvent> channel)
    {
        _serviceChannels[serviceId] = channel;
    }

    // Метод для публикации события из любого сервиса
    public async ValueTask PublishToServiceAsync(string targetServiceId, IApplicationEvent @event)
    {
        if (_serviceChannels.TryGetValue(targetServiceId, out var targetChannel))
        {
            // Пробрасываем событие в канал целевого сервиса
            await targetChannel.Writer.WriteAsync(@event);
        }
        else
        {
            // Логируем, что сервис-получатель не найден или недоступен
            Console.WriteLine($"[GlobalEventHub] Target service '{targetServiceId}' not found.");
        }
    }
}

