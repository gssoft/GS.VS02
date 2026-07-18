using System.Reflection;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Здесь регистрируются ваши модули и GraphTraverser
        services.AddHostedService<GraphTraverser>();
        services.AddKeyedSingleton<IBackgroundModule, ModuleB>("B");
        // ...
    })
    .Build();

await host.RunAsync();

// Регистрация конкретных реализаций под именными ключами
builder.Services.AddHostedService<GraphTraverser>(); // Наш обходчик
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleB>("B");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleG>("G");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleA>("A");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleC>("C");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleD>("D");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleE>("E");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleF>("F");
builder.Services.AddKeyedSingleton<IBackgroundModule, ModuleH>("H");

// ... остальные модули A, C, D, E, F, H

interface IBackgroundModule : IHostedService { /* Маркерный интерфейс */ }


public class ModuleA : BackgroundService, IBackgroundModule
{
    private readonly ILogger<ModuleA> _logger;

    public ModuleA(ILogger<ModuleA> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Модуль A активен.");

        // Логика модуля А. 
        // Если он корневой диспетчер, он может просто ждать остановки,
        // а всю работу делегировать детям через GraphTraverser.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

class ModuleB : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль B запущен.");
        return Task.CompletedTask;
    }
}
class ModuleC : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль C запущен.");
        return Task.CompletedTask;
    }
}

class ModuleD : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль D запущен.");
        return Task.CompletedTask;
    }
}

class ModuleE : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль E запущен.");
        return Task.CompletedTask;
    }
}

class ModuleF : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль F запущен.");
        return Task.CompletedTask;
    }
}

class ModuleG : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль G запущен.");
        return Task.CompletedTask;
    }
}

class ModuleH : BackgroundService, IBackgroundModule
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Модуль H запущен.");
        return Task.CompletedTask;
    }
}


