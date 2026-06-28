using FractalCell.Core;

// === Создаём простые поведения ===
var logger = new LoggingBehavior("Logger");
var heartbeat = new HeartbeatBehavior();
var processor = new DataProcessorBehavior();

// === Создаём простые ячейки ===
var cellA = new FractalCellPipeline("Cell-A")
    .WithBehavior(heartbeat)
    .WithBehavior(logger);

var cellB = new FractalCellPipeline("Cell-B")
    .WithBehavior(processor)
    .WithBehavior(logger);

var cellC = new FractalCellPipeline("Cell-C")
    .WithBehavior(processor);

// 🌳 СОЗДАЁМ ФРАКТАЛЬНУЮ КОМПОЗИЦИЮ
// Cluster-1 содержит Cell-A и Cell-B
var cluster1 = new FractalComposition("Cluster-1", cellA, cellB);

// MegaCluster содержит Cluster-1 и Cell-C
// Cell-C и Cluster-1 — РАВНОПРАВНЫЕ дети, хотя Cluster-1 сам является композицией!
var megaCluster = new FractalComposition("MegaCluster", cluster1, cellC);

Console.WriteLine("=== Структура системы ===");
Console.WriteLine(megaCluster.Dump());

await megaCluster.StartAsync(CancellationToken.None);

// 🎯 Посылаем событие В ГЛУБИНУ структуры, не зная о ней!
// TargetCellId = "Cell-A", но мы шлём в MegaCluster — 
// система сама найдёт Cell-A через Cluster-1.
var ev1 = new SimpleEvent("ev-1", "Cell-A", "Hello from outside!");
await megaCluster.SendAsync(ev1);

// Посылаем всем (без TargetCellId) — обработают собственные поведения MegaCluster
var ev2 = new SimpleEvent("ev-2", null, "Broadcast to all");
await megaCluster.SendAsync(ev2);

Console.ReadLine();

// === Простые реализации для примера ===
record SimpleEvent(string EventId, string? TargetCellId, string Data) : IApplicationEvent;

class LoggingBehavior : BehaviorBlock
{
    public LoggingBehavior(string id) : base(id) { }

    protected override Task<IApplicationEvent> ProcessAsync(
        IApplicationEvent @event, IFractalCell hostCell, CancellationToken ct)
    {
        Console.WriteLine($"  📝 [{hostCell.CellId}] Log: {@event.EventId}");
        return Task.FromResult(@event);
    }
}

class HeartbeatBehavior : BehaviorBlock
{
    public HeartbeatBehavior() : base("Heartbeat") { }

    protected override Task<IApplicationEvent> ProcessAsync(
        IApplicationEvent @event, IFractalCell hostCell, CancellationToken ct)
    {
        Console.WriteLine($"  ❤️  [{hostCell.CellId}] Heartbeat: {@event.EventId}");
        return Task.FromResult(@event);
    }
}

class DataProcessorBehavior : BehaviorBlock
{
    public DataProcessorBehavior() : base("Processor", maxParallelism: 4) { }

    protected override async Task<IApplicationEvent> ProcessAsync(
        IApplicationEvent @event, IFractalCell hostCell, CancellationToken ct)
    {
        Console.WriteLine($"  ⚙️  [{hostCell.CellId}] Processing: {@event.EventId}");
        await Task.Delay(50, ct);
        return @event;
    }
}

