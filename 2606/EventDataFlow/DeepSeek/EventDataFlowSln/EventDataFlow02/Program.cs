// Program.cs

var builder = Host.CreateApplicationBuilder(args);

// Регистрируем EventHub'ы
builder.Services.AddSingleton<IFractalEventHub>(sp =>
    new UniversalFractalEventHub(
        sp.GetRequiredService<ILogger<UniversalFractalEventHub>>(),
        "root-hub"
    ));

// Регистрируем MicroEventBus как singleton (если нужен общий)
builder.Services.AddSingleton<MicroEventBus>();

// Регистрируем ячейки с явным созданием через фабрику
builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<ProducerCell>>();
    return new ProducerCell(
        "producer-1",
        new CellAddress(0, "producer-1"),
        hub,
        logger
    );
});

builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<ConsumerCell>>();
    return new ConsumerCell(
        "consumer-1",
        new CellAddress(0, "consumer-1"),
        hub,
        logger
    );
});

builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<RouterCell>>();
    var router = new RouterCell(
        "router-1",
        new CellAddress(0, "router-1"),
        hub,
        logger
    );

    // Настраиваем маршруты
    router.AddRoute("DataGenerated", "consumer-1");
    router.AddRoute("Heartbeat", "consumer-1");

    return router;
});

var host = builder.Build();
await host.RunAsync();

//var builder = Host.CreateApplicationBuilder(args);

//// Регистрируем EventHub'ы
//builder.Services.AddSingleton<IFractalEventHub>(sp =>
//    new UniversalFractalEventHub(
//        sp.GetRequiredService<ILogger<UniversalFractalEventHub>>(),
//        "root-hub"
//    ));

//// Регистрируем ячейки
//builder.Services.AddHostedService<ProducerCell>();
//builder.Services.AddHostedService<ConsumerCell>();
//builder.Services.AddHostedService<RouterCell>();

//var host = builder.Build();  // <--- Программа падает здесь
//await host.RunAsync();

/*

var builder = WebApplication.CreateBuilder(args);

// Регистрируем корневой EventHub как синглтон
builder.Services.AddSingleton<IFractalEventHub>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<UniversalFractalEventHub>>();
    return new UniversalFractalEventHub(logger, "root-hub");
});

// Регистрируем ячейки как Hosted Services
builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<ProducerCell>>();
    return new ProducerCell("producer-1", new CellAddress(0, "producer-1"), hub, logger);
});

builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<ConsumerCell>>();
    return new ConsumerCell("consumer-1", new CellAddress(0, "consumer-1"), hub, logger);
});

builder.Services.AddHostedService(sp =>
{
    var hub = sp.GetRequiredService<IFractalEventHub>();
    var logger = sp.GetRequiredService<ILogger<RouterCell>>();
    var router = new RouterCell("router-1", new CellAddress(0, "router-1"), hub, logger);

    // Настраиваем маршруты
    router.AddRoute("DataGenerated", "consumer-1");
    router.AddRoute("Heartbeat", "consumer-1");

    return router;
});

var app = builder.Build();
app.Run();
*/
