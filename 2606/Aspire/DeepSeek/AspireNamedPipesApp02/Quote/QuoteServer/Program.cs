using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuoteServer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults(); // из QuoteServiceDefaults

// Регистрируем наши сервисы
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton(sp =>
{
    var port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "5555");
    return new TcpQuoteServer(port, sp.GetRequiredService<EventHub>(), sp.GetRequiredService<ILogger<TcpQuoteServer>>());
});
builder.Services.AddHostedService<QuoteGeneratorHost>();

var host = builder.Build();

// Запускаем TCP сервер в фоне
var tcpServer = host.Services.GetRequiredService<TcpQuoteServer>();
var serverTask = tcpServer.StartAsync(default); // не ждём завершения

await host.RunAsync();

//using QuoteServer;

//var builder = Host.CreateApplicationBuilder(args);

//builder.AddServiceDefaults();
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
