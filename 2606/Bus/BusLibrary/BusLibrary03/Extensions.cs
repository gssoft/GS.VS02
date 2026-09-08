// Extensions.cs (обновленная версия)

using BusLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BusLibrary;
// Updates 26.05.22
public static class EventHubServiceCollectionExtensions
{
    public static IServiceCollection AddEventHub(this IServiceCollection services,
        Action<EventHubOptions>? configure = null)
    {
        var opts = new EventHubOptions();
        configure?.Invoke(opts);

        // Собираем все сборки, в которых искать обработчики
        var assemblies = new List<Assembly>();

        // Добавляем сборки из опций
        if (opts.Assemblies?.Any() == true)
        {
            assemblies.AddRange(opts.Assemblies);
        }

        // Если сборки не указаны, добавляем сборку вызывающего кода
        if (!assemblies.Any())
        {
            assemblies.Add(Assembly.GetCallingAssembly());
        }

        // Создаем реестр обработчиков
        var registry = new EventHandlerRegistry(assemblies);

        // Регистрируем реестр в DI
        services.AddSingleton<IEventHandlerRegistry>(registry);

        // Регистрируем обработчики в DI
        registry.RegisterHandlers(services);

        // Регистрируем менеджер динамических подписок
        services.AddSingleton<DynamicSubscriptionManager>();
        services.AddSingleton<IDynamicSubscriptionManager>(sp =>
            sp.GetRequiredService<DynamicSubscriptionManager>());

        // Регистрируем композитный роутер
        // services.AddSingleton<IKeyRouter, CompositeKeyRouter>();    // !!!!!!!!!!!!!!
        // Updates 26.05.22
        switch (opts.RoutingMode)
        {
            case RoutingMode.Static:
                services.AddSingleton<IKeyRouter, StaticKeyRouter>();
                break;
            case RoutingMode.Dynamic:
                services.AddSingleton<IKeyRouter, DynamicKeyRouter>();
                break;
            // Updates 26.05.22
            case RoutingMode.Both:
            default:
                services.AddSingleton<IKeyRouter, CompositeKeyRouter>();
                break;
        }

        if (opts.Catalog is not null)
        {
            services.AddSingleton<IKeyCatalog>(opts.Catalog);
        }

        // Регистрируем EventHub
        services.AddSingleton<IEventHub>(serviceProvider =>
        {
            var router = serviceProvider.GetRequiredService<IKeyRouter>();
            var catalog = serviceProvider.GetService<IKeyCatalog>();
            var logger = serviceProvider.GetService<ILogger<InProcessEventHub>>();

            return new InProcessEventHub(serviceProvider, router, catalog,
                opts.ChannelCapacity, logger);
        });

        return services;
    }

    public sealed class EventHubOptions
    {
        public int ChannelCapacity { get; set; } = 8192;
        public IKeyCatalog? Catalog { get; set; }
        public IReadOnlyCollection<Assembly>? Assemblies { get; set; }

        // Updates 26.05.22
        public RoutingMode RoutingMode { get; set; } = RoutingMode.Both;

    }

    // Updates 26.05.22
    public static IServiceCollection AddStaticEventHub(this IServiceCollection services,
    Action<EventHubOptions>? configure = null)
    {
        return services.AddEventHub(opts =>
        {
            opts.RoutingMode = RoutingMode.Static;
            configure?.Invoke(opts);
        });
    }

    // Updates 26.05.22
    public static IServiceCollection AddDynamicEventHub(this IServiceCollection services,
        Action<EventHubOptions>? configure = null)
    {
        return services.AddEventHub(opts =>
        {
            opts.RoutingMode = RoutingMode.Dynamic;
            configure?.Invoke(opts);
        });
    }
}
