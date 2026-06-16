// Program.cs

using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Канал для шины сообщений
        // services.AddSingleton(Channel.CreateUnbounded<IMessage>());

        services.AddSingleton(Channel.CreateBounded<IMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait // Ожидание, если канал полон
        }));

        // Регистрация шины сообщений
        services.AddSingleton<IMessageBus, MessageBus>();

        // Регистрация маршрутизатора
        services.AddHostedService<MessageRouter>();

        // Регистрация Background Services
        services.AddHostedService<Service1>();
        services.AddHostedService<Service2>();
        services.AddHostedService<Service3>();

        // Регистрация обработчиков
        services.AddTransient<IHandler<SomeCommand>, SomeCommandHandler>();
        services.AddTransient<IHandler<AnotherCommand>, AnotherCommandHandler>();
        services.AddTransient<IHandler<SomeEvent>, SomeEventHandler>();
    });

await builder.Build().RunAsync();

