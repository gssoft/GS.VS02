// Behaviors/HeartbeatBehavior.cs

using FractalCellCore;
using FractalCellCore.Core.Interfaces;
using FractalCellCore.Core.Templates;
using Microsoft.Extensions.Logging;

namespace FractalCellApp.Behaviors;

/// <summary>
/// Поведение для обработки heartbeat-событий
/// </summary>
//public class HeartbeatBehavior : EventBehaviorTemplate<FractalEvent>
//{
//    private int _heartbeatCount;
//    private readonly TimeSpan _heartbeatInterval;

//    public override string BehaviorId => "HeartbeatBehavior";

//    // Добавляем приоритет (высокий)
//    public override int Priority => 10;

//    public HeartbeatBehavior(ILogger<HeartbeatBehavior>? logger = null, TimeSpan? interval = null)
//        : base(logger)
//    {
//        _heartbeatInterval = interval ?? TimeSpan.FromSeconds(5);
//    }

//    public HeartbeatBehavior() : this(null, null)
//    {
//    }

//    protected override async Task HandleEventAsync(FractalEvent @event)
//    {
//        if (@event.EventType != "Heartbeat")
//            return;

//        _heartbeatCount++;

//        _logger?.LogInformation(
//            "❤️ [Heartbeat] Count: {Count}, Source: {Source}, Target: {Target}, Timestamp: {Timestamp}",
//            _heartbeatCount,
//            @event.SourceCellId,
//            @event.TargetCellId,
//            @event.Timestamp);

//        if (_heartbeatCount % 3 == 0 && _attachedCell != null)
//        {
//            _logger?.LogInformation("🔄 [Heartbeat] Sending response from {CellId}",
//                _attachedCell.CellId);

//            var responseEvent = new FractalEvent(
//                $"response-{DateTime.UtcNow.Ticks}",
//                DateTime.UtcNow,
//                _attachedCell.CellId,
//                @event.SourceCellId,
//                "HeartbeatResponse",
//                new
//                {
//                    ReceivedAt = DateTime.UtcNow,
//                    HeartbeatCount = _heartbeatCount
//                }
//            );

//            await _attachedCell.ExternalBus.SendToCellAsync(
//                @event.SourceCellId,
//                responseEvent
//            );
//        }
//    }
//}
