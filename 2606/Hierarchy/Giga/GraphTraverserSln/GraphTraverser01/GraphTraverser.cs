using System.Reflection;

public sealed class GraphTraverser : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GraphTraverser> _logger;

    // Корневые узлы графа (в вашем примере это A)
    private readonly List<ModuleNode> _rootNodes = new()
    {
        new ModuleNode
        {
            Name = "A",
            ServiceType = typeof(ModuleA),
            Children = new()
            {
                new ModuleNode { Name = "B", ServiceType = typeof(ModuleB), Children = new() {
                    new ModuleNode { Name = "D", ServiceType = typeof(ModuleD) },
                    new ModuleNode { Name = "E", ServiceType = typeof(ModuleE) }
                }},
                new ModuleNode { Name = "C", ServiceType = typeof(ModuleC), Children = new() {
                    new ModuleNode { Name = "F", ServiceType = typeof(ModuleF) },
                    new ModuleNode { Name = "G", ServiceType = typeof(ModuleG) },
                    new ModuleNode { Name = "H", ServiceType = typeof(ModuleH) }
                }}
            }
        }
    };

    public GraphTraverser(IServiceScopeFactory scopeFactory, ILogger<GraphTraverser> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Запуск обхода графа модулей...");

        // Запускаем обход каждого корневого узла
        foreach (var root in _rootNodes)
        {
            await TraverseAndRun(root, stoppingToken);
        }

        _logger.LogInformation("Обход графа завершен.");
    }

    private async Task TraverseAndRun(ModuleNode node, CancellationToken token)
    {
        if (node == null) return;

        // Создаем изолированный Scope для текущего узла
        using var scope = _scopeFactory.CreateScope();

        // Высвечиваем наименование Node (как требовалось в задаче)
        _logger.LogInformation($"--> Обработка узла: [{node.Name}]");

        // Динамически получаем экземпляр нужного BackgroundService из контейнера по ключу
        var module = (IBackgroundModule?)scope.ServiceProvider.GetKeyedService(typeof(IBackgroundModule), node.Name);

        if (module != null)
        {
            // Запускаем модуль. В реальном сценарии здесь может быть не прямой запуск,
            // а постановка задачи в очередь этого модуля.
            await module.StartAsync(token);
        }

        // Рекурсивно обходим детей (DFS - в глубину)
        foreach (var child in node.Children)
        {
            await TraverseAndRun(child, token);
        }

        // Останавливаем модуль после обработки ветки (опционально)
        if (module != null)
        {
            await module.StopAsync(token);
        }
    }
}
