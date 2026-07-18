//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//public class ModuleA : BackgroundService, IBackgroundModule
//{
//    private readonly ILogger<ModuleA> _logger;

//    public ModuleA(ILogger<ModuleA> logger)
//    {
//        _logger = logger;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Модуль A активен.");

//        // Логика модуля А. 
//        // Если он корневой диспетчер, он может просто ждать остановки,
//        // а всю работу делегировать детям через GraphTraverser.
//        await Task.Delay(Timeout.Infinite, stoppingToken);
//    }
//}


//public class ModuleB : BackgroundService, IBackgroundModule
//{
//    private readonly ILogger<ModuleB> _logger;

//    public ModuleB(ILogger<ModuleB> logger)
//    {
//        _logger = logger;
//    }

//    protected override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation(">>> Модуль B: Выполняю специфическую задачу...");
//        return Task.CompletedTask;
//    }
//}