// using BackgroundServicesMediatR.Handlers;
// using BackgroundServicesMediatR.Services;

using MediatR;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Регистрируем MediatR и автоматически обнаруживаем обработчики
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PingMessageHandler>());

        // Регистрируем Background Services
        services.AddHostedService<PingService>();
        services.AddHostedService<PongService>();

        // Добавляем логирование
        services.AddLogging();
    });

var host = builder.Build();
await host.RunAsync();


//using BackgroundServicesMediatR;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
