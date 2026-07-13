using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Регистрируем нашу "очередь" (в реальности это Redis/RabbitMQ)
        services.AddSingleton<IMessageQueue, InMemoryQueue>();
    })
    .Build();

await host.RunAsync();
