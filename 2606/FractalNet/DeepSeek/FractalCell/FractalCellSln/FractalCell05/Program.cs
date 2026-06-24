using FractalCell02;
using FractalCell02.Core.Configuration;
using FractalCell02.Core.Interfaces;
using FractalCell02.Implementations;
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

// Просто запускаем хост и ждем его завершения
await host.RunAsync();

Console.WriteLine("✅ Fractal System stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Fractal System started successfully!");
//Console.WriteLine("Press Enter to stop...");

//// Ждем нажатия Enter
//Console.ReadLine();

//Console.WriteLine("🛑 Stopping application...");

//// Останавливаем хост
//await host.StopAsync();

//Console.WriteLine("✅ Fractal System stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Fractal System started successfully!");

//// Просто ждем нажатия Enter
//Console.WriteLine("Press Enter to stop...");
//Console.ReadLine();

//Console.WriteLine("🛑 Stopping application...");

//// Останавливаем хост
//await host.StopAsync();

//Console.WriteLine("✅ Fractal System stopped");

//// Worker оркестратор

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//// Work well

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
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

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Enter to stop");

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started successfully!");

//// Ждем нажатия Enter
//Console.ReadLine();

//Console.WriteLine("🛑 Stopping application...");

//// Останавливаем хост
//await host.StopAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Enter to stop");

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started successfully!");

//// Ждем нажатия Enter
//Console.ReadLine();

//Console.WriteLine("🛑 Stopping application...");

//// Останавливаем хост
//await host.StopAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Enter to stop");

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started successfully!");

//// Ждем нажатия Enter
//Console.ReadLine();

//Console.WriteLine("🛑 Stopping application...");

//// Останавливаем хост
//await host.StopAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//// Просто запускаем хост
//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//Console.WriteLine("🚀 Starting application...");
//Console.WriteLine("ℹ️ Press Ctrl+C to stop");

//// Запускаем хост
//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//// Просто запускаем хост и ждем его завершения
//await host.RunAsync();

//Console.WriteLine("✅ Application stopped");

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//// Подписываемся на события остановки для отладки
//var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
//lifetime.ApplicationStopping.Register(() =>
//{
//    Console.WriteLine("⚠️⚠️⚠️ APPLICATION STOPPING TRIGGERED! ⚠️⚠️⚠️");
//    Console.WriteLine($"Stack trace: {Environment.StackTrace}");
//});

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started. Press Enter to stop...");

//// Ждем нажатия Enter
//Console.ReadLine();

//// Останавливаем хост
//await host.StopAsync();
//await host.WaitForShutdownAsync();

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//// Подписываемся на события остановки для отладки
//var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
//lifetime.ApplicationStopping.Register(() =>
//{
//    Console.WriteLine("⚠️⚠️⚠️ APPLICATION STOPPING TRIGGERED! ⚠️⚠️⚠️");
//});

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started. Press Enter to stop...");

//// Ждем нажатия Enter
//Console.ReadLine();

//// Останавливаем хост
//await host.StopAsync();

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//// Запускаем хост и ждем его завершения
//await host.RunAsync();

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//// Запускаем хост
//await host.StartAsync();

//Console.WriteLine("✅ Application started. Press Enter to stop...");

//// Ждем нажатия Enter
//Console.ReadLine();

//// Останавливаем хост
//await host.StopAsync();
//await host.WaitForShutdownAsync();

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования - ВАЖНО: установить уровень Information
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//await host.RunAsync();

//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

//// Настройка логирования
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

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//await host.RunAsync();

////using FractalCell02;
////using FractalCell02.Core.Configuration;
////using FractalCell02.Core.Interfaces;
////using FractalCell02.Implementations;
////using Microsoft.Extensions.DependencyInjection;
////using Microsoft.Extensions.Hosting;
////using Microsoft.Extensions.Logging;
//using FractalCell02;
//using FractalCell02.Core.Configuration;
//using FractalCell02.Core.Interfaces;
//using FractalCell02.Implementations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//var builder = Host.CreateApplicationBuilder(args);

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

//builder.Services.AddLogging(configure =>
//{
//    configure.ClearProviders();
//    configure.AddConsole();
//    configure.AddDebug();
//    configure.SetMinimumLevel(LogLevel.Debug); // Добавьте это для отладки
//});

//var host = builder.Build();

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//await host.RunAsync();


//var builder = Host.CreateApplicationBuilder(args);

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

//builder.Services.AddLogging(configure =>
//{
//    configure.ClearProviders();
//    configure.AddConsole();
//    configure.AddDebug();

//    if (builder.Environment.IsDevelopment())
//    {
//        configure.AddConsole();
//    }
//});

//var host = builder.Build();

//AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//{
//    var logger = host.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
//};

//await host.RunAsync();