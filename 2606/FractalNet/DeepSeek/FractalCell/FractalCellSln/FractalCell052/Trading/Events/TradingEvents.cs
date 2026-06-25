using FractalCell02.Core.Interfaces;

namespace FractalCell02.Trading.Events;

public enum OrderSide { Buy, Sell }
public enum OrderType { Market, Limit }

/// <summary>
/// Котировка (Тик) — рассылается генератором всем через Broadcast
/// </summary>
public record QuoteEvent(
    string EventId,
    DateTime Timestamp,
    string Symbol,
    decimal Bid,
    decimal Ask,
    string SourceCellId = ""
) : IApplicationEvent;

/// <summary>
/// Заявка на торговлю — отправляется клиентом в MatchingEngine
/// </summary>
public record OrderEvent(
    string EventId,
    DateTime Timestamp,
    string OrderId,
    string ClientCellId,     // CellId портфеля-отправителя
    string Symbol,
    decimal Price,
    decimal Quantity,
    OrderSide Side,
    OrderType Type,
    string TargetCellId = ""  // CellId MatchingEngine
) : IApplicationEvent;

/// <summary>
/// Сделка — генерируется MatchingEngine при исполнении ордера
/// </summary>
public record TradeEvent(
    string EventId,
    DateTime Timestamp,
    string TradeId,
    string OrderId,
    string ClientCellId,
    string Symbol,
    decimal ExecutedPrice,
    decimal Quantity,
    OrderSide Side,
    string SourceCellId = ""
) : IApplicationEvent;

/// <summary>
/// Обновление позиции — отправляется портфелем самому себе (или в лог)
/// </summary>
public record PositionUpdateEvent(
    string EventId,
    DateTime Timestamp,
    string ClientCellId,
    string Symbol,
    decimal NewQuantity,
    decimal AveragePrice,
    decimal CashBalance
) : IApplicationEvent;
