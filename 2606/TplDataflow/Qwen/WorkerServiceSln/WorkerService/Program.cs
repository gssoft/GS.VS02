using WorkerService.Dataflow;
using WorkerService.Services;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Регистрируем QuoteChannel как Singleton (один на все сервисы)
        services.AddSingleton<QuoteChannel>();

        // Регистрируем наши BackgroundServices
        services.AddHostedService<PublisherService>();
        services.AddHostedService<SubscriberService>();
        services.AddHostedService<MonitoringService>();
    });

var host = builder.Build();
await host.RunAsync();
