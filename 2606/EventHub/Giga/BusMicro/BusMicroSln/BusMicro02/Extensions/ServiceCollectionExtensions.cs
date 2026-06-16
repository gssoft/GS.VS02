// BusMicro/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;

namespace BusMicro;

public static class ServiceCollectionExtensions
{
    // Метод теперь принимает канал как параметр
    public static void AddBusMicro(this IServiceCollection services, Channel<IMessage> channel)
    {
        // Регистрируем Reader и Writer, используя переданный канал
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);

        // Регистрируем компоненты
        services.AddTransient<IMessageBus, MessageBus>();
        services.AddHostedService<MessageRouter>();
    }
}


//// BusMicro/ServiceCollectionExtensions.cs
//using Microsoft.Extensions.DependencyInjection;
//using System.Threading.Channels;

//namespace BusMicro;

//public static class ServiceCollectionExtensions
//{
//    public static void AddBusMicro(this IServiceCollection services)
//    {
//        // 1. Создаем сам канал как Singleton.
//        // Он будет жить на протяжении всего времени работы приложения.
//        var channel = Channel.CreateUnbounded<IMessage>();

//        // 2. Регистрируем Reader и Writer как Singleton,
//        // указывая, что они должны использовать наш созданный канал.
//        // Это позволяет внедрять их по отдельности в нужные классы.
//        services.AddSingleton(channel.Reader);
//        services.AddSingleton(channel.Writer);

//        // 3. Регистрируем компоненты шины.
//        // IMessageBus будет создавать новые экземпляры при каждом запросе (Transient).
//        services.AddTransient<IMessageBus, MessageBus>();

//        // MessageRouter будет запущен как фоновый сервис (HostedService).
//        services.AddHostedService<MessageRouter>();
//    }
//}
