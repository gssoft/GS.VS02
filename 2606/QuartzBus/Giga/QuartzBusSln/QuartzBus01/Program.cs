builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MyBusinessWorker>(); // Регистрируем нашего работника-потребителя
    // Настройка самого брокера (например, RabbitMQ localhost)
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost");
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionScopedJobFactory();

    // Настраиваем триггеры, как и раньше
    q.AddTrigger(t => t
        .ForJob<SchedulerJob>()
        .WithIdentity("MorningStartTrigger")
        .WithCronSchedule("0 0 9 ? * MON-FRI"));

    q.AddTrigger(t => t
        .ForJob<SchedulerJob>()
        .WithIdentity("EveningStopTrigger")
        .WithCronSchedule("0 0 18 ? * MON-FRI"));
});

builder.Services.AddQuartzHostedService();

//using QuartzBus01;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
