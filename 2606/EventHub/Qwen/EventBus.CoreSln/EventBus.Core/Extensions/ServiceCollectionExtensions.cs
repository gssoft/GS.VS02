// Extensions/ServiceCollectionExtensions.cs

using System.Threading.Channels;
using EventBus.Abstractions;
using EventBus.Core;
using EventBus.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Регистрирует шину событий.</summary>
    public static IServiceCollection AddChannelEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, ChannelEventBus>();
        services.AddSingleton<ChannelEventBus>(); // Для доступа к .Subscribe()
        return services;
    }

    /// <summary>
    /// Регистрирует канал, сервис-подписчик и автоматически настраивает буфер.
    /// </summary>
    public static IServiceCollection AddSubscriber<TEvent, TService>(
        this IServiceCollection services,
        int bufferSize = 1000,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.DropOldest)
        where TEvent : class
        where TService : EventSubscriberService<TEvent>
    {
        services.AddSingleton<Channel<TEvent>>(_ =>
            Channel.CreateBounded<TEvent>(new BoundedChannelOptions(bufferSize)
            {
                FullMode = fullMode,
                SingleReader = true,
                SingleWriter = false
            }));

        services.AddHostedService<TService>();
        return services;
    }
}
