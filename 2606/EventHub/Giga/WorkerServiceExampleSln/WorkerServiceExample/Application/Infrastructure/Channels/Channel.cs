// Application/Infrastructure/Channels/Channel.cs

using System.Threading.Channels;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Channels;

// Регистрируем канал как синглтон с помощью хоста-хелпера
public static class CommandChannel
{
    public static void AddCommandChannel(this IServiceCollection services)
    {
        var channel = Channel.CreateBounded<ICommand>(new BoundedChannelOptions(100)
        {
            SingleReader = true, // Один роутер на все команды
            FullMode = BoundedChannelFullMode.Wait // Блокировать отправителя, если канал полон
        });
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
    }
}

// Аналогично для событий, но позволим несколько читателей для параллелизма
public static class EventChannel
{
    public static void AddEventChannel(this IServiceCollection services)
    {
        var channel = Channel.CreateBounded<IEvent>(new BoundedChannelOptions(500)
        {
            SingleReader = false, // Можем запустить несколько роутеров для событий
            FullMode = BoundedChannelFullMode.DropOldest // Если канал полон, удаляем самое старое событие (логика для логов/аналитики)
        });
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);
    }
}
