// FractalCellApp/Program.cs

using FractalBehaviors;
using FractalCellApp;

using FractalCellCore.Core.Configuration;

// using FractalCellApp.Behaviors;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Implementations;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Настройка конфигурации
builder.Services.Configure<FractalCellConfiguration>(
    builder.Configuration.GetSection("FractalCell"));

// Регистрация Hub
builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>();
builder.Services.AddSingleton<HubSettings>(sp => new HubSettings
{
    ChannelCapacity = 1000,
    EnablePersistence = false,
    MessageTimeout = TimeSpan.FromSeconds(30)
});

// Регистрация поведений как Singleton для DI
//builder.Services.AddSingleton<HeartbeatBehavior>();
//builder.Services.AddSingleton<DataProcessingBehavior>();

// Регистрация Worker с поведением
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

Console.WriteLine("🚀 Starting Fractal System with Behaviors...");
Console.WriteLine("ℹ️ Press Ctrl+C to stop");
Console.WriteLine("🎯 Behaviors: HeartbeatBehavior, DataProcessingBehavior");
Console.WriteLine("📊 Events: Heartbeat, ProcessData");
Console.WriteLine();

await host.RunAsync();

Console.WriteLine("✅ Fractal System stopped");



