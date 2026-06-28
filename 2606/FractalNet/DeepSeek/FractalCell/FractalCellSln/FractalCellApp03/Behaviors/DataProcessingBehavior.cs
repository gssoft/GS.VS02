// //Behaviors/ DataProcessingBehavior.cs

using System.Diagnostics;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellApp.Behaviors;

/// <summary>
/// Поведение для обработки данных
/// </summary>
public class DataProcessingBehavior : EventBehaviorTemplate<FractalEvent>, ILifecycleBehavior
{
    private readonly Stopwatch _stopwatch = new();
    private int _processedCount;
    private readonly int _maxParallelProcessing;

    public override string BehaviorId => "DataProcessingBehavior";

    public DataProcessingBehavior(
        ILogger<DataProcessingBehavior>? logger = null,
        int maxParallelProcessing = 3)
        : base(logger)
    {
        _maxParallelProcessing = maxParallelProcessing;
    }

    // ✅ Добавляем конструктор без параметров для Activator.CreateInstance
    public DataProcessingBehavior() : this(null, 3)
    {
    }

    protected override async Task HandleEventAsync(FractalEvent @event)
    {
        if (@event.EventType != "ProcessData")
            return;

        Interlocked.Increment(ref _processedCount);

        _logger?.LogInformation(
            "📊 [DataProcessing] Processing data: EventId={EventId}, Source={Source}, Payload={Payload}",
            @event.EventId,
            @event.SourceCellId,
            @event.Payload);

        // Имитация обработки данных
        await Task.Delay(100);

        _logger?.LogInformation(
            "✅ [DataProcessing] Data processed successfully: {EventId}",
            @event.EventId);
    }

    public async Task OnCellStartingAsync(CancellationToken ct)
    {
        _logger?.LogInformation("🚀 [DataProcessing] Cell starting, resetting counters");
        Interlocked.Exchange(ref _processedCount, 0);
        await Task.CompletedTask;
    }

    public async Task OnCellStartedAsync(CancellationToken ct)
    {
        _logger?.LogInformation("✅ [DataProcessing] Cell started, ready to process data");
        _stopwatch.Start();
        await Task.CompletedTask;
    }

    public async Task OnCellStoppingAsync(CancellationToken ct)
    {
        _logger?.LogInformation("⏹️ [DataProcessing] Cell stopping, final stats: {Count} items processed",
            _processedCount);
        await Task.CompletedTask;
    }

    public async Task OnCellStoppedAsync(CancellationToken ct)
    {
        _stopwatch.Stop();
        _logger?.LogInformation("🏁 [DataProcessing] Cell stopped. Total time: {Elapsed}, Items: {Count}",
            _stopwatch.Elapsed, _processedCount);
        await Task.CompletedTask;
    }
}

// ----------
// Этот был без ошибок  VVVVVVVVVVV

//using System.Diagnostics;
//using FractalCellCore.Core.Interfaces;
//using FractalCellCore.Core.Templates;
//using Microsoft.Extensions.Logging;

//namespace FractalCellApp.Behaviors;

///// <summary>
///// Поведение для обработки данных
///// </summary>
//public class DataProcessingBehavior : EventBehaviorTemplate<FractalEvent>, ILifecycleBehavior
//{
//    private readonly Stopwatch _stopwatch = new();
//    private int _processedCount;
//    private readonly int _maxParallelProcessing;

//    public override string BehaviorId => "DataProcessingBehavior";

//    public DataProcessingBehavior(
//        ILogger<DataProcessingBehavior>? logger = null,
//        int maxParallelProcessing = 3)
//        : base(logger)
//    {
//        _maxParallelProcessing = maxParallelProcessing;
//    }

//    protected override async Task HandleEventAsync(FractalEvent @event)
//    {
//        if (@event.EventType != "ProcessData")
//            return;

//        Interlocked.Increment(ref _processedCount);

//        _logger?.LogInformation(
//            "📊 [DataProcessing] Processing data: EventId={EventId}, Source={Source}, Payload={Payload}",
//            @event.EventId,
//            @event.SourceCellId,
//            @event.Payload);

//        // Имитация обработки данных
//        await Task.Delay(100);

//        _logger?.LogInformation(
//            "✅ [DataProcessing] Data processed successfully: {EventId}",
//            @event.EventId);
//    }

//    public async Task OnCellStartingAsync(CancellationToken ct)
//    {
//        _logger?.LogInformation("🚀 [DataProcessing] Cell starting, resetting counters");
//        Interlocked.Exchange(ref _processedCount, 0);
//        await Task.CompletedTask;
//    }

//    public async Task OnCellStartedAsync(CancellationToken ct)
//    {
//        _logger?.LogInformation("✅ [DataProcessing] Cell started, ready to process data");
//        _stopwatch.Start();
//        await Task.CompletedTask;
//    }

//    public async Task OnCellStoppingAsync(CancellationToken ct)
//    {
//        _logger?.LogInformation("⏹️ [DataProcessing] Cell stopping, final stats: {Count} items processed",
//            _processedCount);
//        await Task.CompletedTask;
//    }

//    public async Task OnCellStoppedAsync(CancellationToken ct)
//    {
//        _stopwatch.Stop();
//        _logger?.LogInformation("🏁 [DataProcessing] Cell stopped. Total time: {Elapsed}, Items: {Count}",
//            _stopwatch.Elapsed, _processedCount);
//        await Task.CompletedTask;
//    }
//}
