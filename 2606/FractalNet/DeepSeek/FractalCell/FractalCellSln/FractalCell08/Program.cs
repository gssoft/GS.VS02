using FractalCell;
using FractalCell.Core.Configuration;
using FractalCell.Implementations;

using FractalCell.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

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

var host = builder.Build();

Console.WriteLine("🚀 Starting Fractal System...");
Console.WriteLine("ℹ️ Press Ctrl+C to stop");

await host.RunAsync();

Console.WriteLine("✅ Fractal System stopped");

// using FractalCell08;
// using FractalCell08.Core.Configuration;
// using FractalCell08.Implementations;

// namespace FractalCell;

//using FractalCell;

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddDebug();
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//builder.Services.Configure<FractalCellConfiguration>(
//    builder.Configuration.GetSection("FractalCell"));

//builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>();
//builder.Services.AddSingleton<HubSettings>(sp => new HubSettings
//{
//    ChannelCapacity = 1000,
//    EnablePersistence = false,
//    MessageTimeout = TimeSpan.FromSeconds(30)
//});

//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();

//Console.WriteLine("🚀 Starting Fractal System...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//await host.RunAsync();

//Console.WriteLine("✅ Fractal System stopped");

