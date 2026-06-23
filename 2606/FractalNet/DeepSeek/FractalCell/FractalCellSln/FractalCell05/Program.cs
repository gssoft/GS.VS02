using FractalCell02;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<FractalCellConfiguration>(
    builder.Configuration.GetSection("FractalCell"));

builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>();
builder.Services.AddSingleton<HubSettings>(sp => new HubSettings
{
    ChannelCapacity = 1000,
    EnablePersistence = false,
    MessageTimeout = TimeSpan.FromSeconds(30)
});

builder.Services.AddHostedService<Worker>();

builder.Services.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
    configure.AddDebug();

    if (builder.Environment.IsDevelopment())
    {
        configure.AddConsole();
    }
});

var host = builder.Build();

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
};

await host.RunAsync();