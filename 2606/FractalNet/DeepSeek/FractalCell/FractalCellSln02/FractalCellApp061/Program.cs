// Program.cs
using FractalBehaviors;
using FractalCellApp;
using FractalCellCore.Core.Configuration;
using FractalCellCore.Core.DI;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configuration binding (optional)
builder.Services.Configure<FractalCellConfiguration>(
    builder.Configuration.GetSection("FractalCell"));

// === Core infrastructure registrations ===

// Register the Fractal Event Hub (required by node factories)
builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>();

// Hub settings (example)
builder.Services.AddSingleton<HubSettings>(_ => new HubSettings
{
    ChannelCapacity = 1000,
    EnablePersistence = false,
    MessageTimeout = TimeSpan.FromSeconds(30)
});

// Register node infrastructure: Node registry, factories, TopologyLoader, etc.
// This call registers TopologyLoader and the INodeFactory implementations.
builder.Services.AddFractalNodeInfrastructure();

// Optional: register behaviors in DI if you want to resolve them by type name later
// builder.Services.AddSingleton<HeartbeatBehavior>();
// builder.Services.AddSingleton<DataProcessingBehavior>();
// builder.Services.AddSingleton<TimeGenerationBehavior>();
// builder.Services.AddSingleton<TimeSynchronizationBehavior>();
// builder.Services.AddSingleton<OrchestratorBehavior>();

// Register the Worker (hosted service) that loads topology and starts nodes
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

Console.WriteLine("🚀 Starting Fractal System with Behaviors...");
Console.WriteLine("ℹ️ Press Ctrl+C to stop");
Console.WriteLine();

await host.RunAsync();

Console.WriteLine("✅ Fractal System stopped");


//// FractalCellApp/Program.cs

//using FractalBehaviors;
//using FractalCellApp;

//using FractalCellCore.Core.Configuration;

//// using FractalCellApp.Behaviors;
//using FractalCellCore.Core.Interfaces;
//using FractalCellCore.Implementations;
//using System.Text;

//Console.OutputEncoding = Encoding.UTF8;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddDebug();
//builder.Logging.SetMinimumLevel(LogLevel.Information);

//// Настройка конфигурации
//builder.Services.Configure<FractalCellConfiguration>(
//    builder.Configuration.GetSection("FractalCell"));

//// Регистрация Hub
//builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>();
//builder.Services.AddSingleton<HubSettings>(sp => new HubSettings
//{
//    ChannelCapacity = 1000,
//    EnablePersistence = false,
//    MessageTimeout = TimeSpan.FromSeconds(30)
//});

////// Пример в Program.cs
////builder.Services.AddFractalNodeInfrastructure();
////builder.Services.AddSingleton<IFractalEventHub, InMemoryFractalEventHub>(); // если ещё нет
////builder.Services.AddSingleton<HubSettings>(sp => new HubSettings { ChannelCapacity = 1000, EnablePersistence = false, MessageTimeout = TimeSpan.FromSeconds(30) });


//// Регистрация поведений как Singleton для DI
////builder.Services.AddSingleton<HeartbeatBehavior>();
////builder.Services.AddSingleton<DataProcessingBehavior>();

//// Регистрация Worker с поведением
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();

//Console.WriteLine("🚀 Starting Fractal System with Behaviors...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");
//Console.WriteLine("🎯 Behaviors: HeartbeatBehavior, DataProcessingBehavior");
//Console.WriteLine("📊 Events: Heartbeat, ProcessData");
//Console.WriteLine();


//await host.RunAsync();

//Console.WriteLine("✅ Fractal System stopped");



