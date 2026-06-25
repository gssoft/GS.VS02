// Шаг 8: Поведение — Портфель Клиента 
// Создайте файл Trading / Behaviors / PortfolioBehavior.cs:

using FractalCell02.Trading.Behaviors;
using FractalCell02.Core.Behaviors;
using FractalCell02.Core.Interfaces;
using FractalCell02.Trading.Events;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Trading.Behaviors;

/// <summary>
/// Портфель клиента: отслеживает кэш, позиции и P&L.
/// Получает TradeEvent и обновляет внутреннее состояние.
/// Может генерировать новые OrderEvent на основе стратегии.
/// </summary>
public class PortfolioBehavior : ICellBehavior
{
    private readonly string _clientName;
    private readonly string _matchingEngineCellId;

    private decimal _cash;
    private readonly Dictionary<string, Position> _positions = new();
    private int _orderSequence;

    public record Position(string Symbol, decimal Quantity, decimal AveragePrice);

    public PortfolioBehavior(
        string clientName,
        decimal initialCash,
        string matchingEngineCellId)
    {
        _clientName = clientName;
        _cash = initialCash;
        _matchingEngineCellId = matchingEngineCellId;
    }

    public Task OnStartAsync(ICellContext context)
    {
        context.Logger.LogInformation(
            "💼 [{Client}] Portfolio started. Cash: {Cash:C}",
            _clientName, _cash);
        return Task.CompletedTask;
    }

    public async Task OnMessageAsync(IApplicationEvent @event, ICellContext context)
    {
        switch (@event)
        {
            case TradeEvent trade:
                await HandleTrade(trade, context);
                break;

            case QuoteEvent quote:
                // Портфель может реагировать на котировки (автостратегия)
                await EvaluateStrategy(quote, context);
                break;
        }
    }

    private Task HandleTrade(TradeEvent trade, ICellContext context)
    {
        var cost = trade.ExecutedPrice * trade.Quantity;

        if (trade.Side == OrderSide.Buy)
        {
            if (_cash < cost)
            {
                context.Logger.LogWarning(
                    "⚠️ [{Client}] Insufficient cash for trade {TradeId}. Need {Cost:C}, have {Cash:C}",
                    _clientName, trade.TradeId, cost, _cash);
                return Task.CompletedTask;
            }

            _cash -= cost;
            UpdatePosition(trade.Symbol, trade.Quantity, trade.ExecutedPrice);
        }
        else // Sell
        {
            _cash += cost;
            UpdatePosition(trade.Symbol, -trade.Quantity, trade.ExecutedPrice);
        }

        var pos = _positions.GetValueOrDefault(trade.Symbol);

        context.Logger.LogInformation(
            "💼 [{Client}] TRADE CONFIRMED: {TradeId} | {Side} {Qty} {Symbol} @ {Price} | " +
            "Cash: {Cash:C} | Position: {PosQty} @ {PosAvg:C}",
            _clientName, trade.TradeId, trade.Side, trade.Quantity,
            trade.Symbol, trade.ExecutedPrice,
            _cash, pos?.Quantity ?? 0, pos?.AveragePrice ?? 0);

        return Task.CompletedTask;
    }

    private void UpdatePosition(string symbol, decimal quantityDelta, decimal price)
    {
        if (_positions.TryGetValue(symbol, out var existing))
        {
            var newQty = existing.Quantity + quantityDelta;
            if (newQty == 0)
            {
                _positions.Remove(symbol);
            }
            else
            {
                var newAvg = quantityDelta > 0
                    ? (existing.AveragePrice * existing.Quantity + price * quantityDelta) / newQty
                    : existing.AveragePrice;

                _positions[symbol] = new Position(symbol, newQty, Math.Round(newAvg, 4));
            }
        }
        else if (quantityDelta > 0)
        {
            _positions[symbol] = new Position(symbol, quantityDelta, price);
        }
    }

    /// <summary>
    /// Простая стратегия: если цена упала ниже порога — покупаем
    /// </summary>
    private async Task EvaluateStrategy(QuoteEvent quote, ICellContext context)
    {
        // Демо-стратегия: каждая 10-я котировка отправляет ордер
        _orderSequence++;
        if (_orderSequence % 10 != 0) return;

        // Покупаем только если есть деньги
        if (_cash < quote.Ask * 10) return;

        _orderSequence++;
        var order = new OrderEvent(
            EventId: $"order-{Guid.NewGuid():N}"[..16],
            Timestamp: DateTime.UtcNow,
            OrderId: $"O{_clientName}-{_orderSequence:D4}",
            ClientCellId: context.CellId,
            Symbol: quote.Symbol,
            Price: quote.Ask,
            Quantity: 10,
            Side: OrderSide.Buy,
            OrderType: OrderType.Limit,
            TargetCellId: _matchingEngineCellId);

        context.Logger.LogInformation(
            "📤 [{Client}] AUTO-ORDER: Buy {Qty} {Symbol} @ {Price}",
            _clientName, order.Quantity, order.Symbol, order.Price);

        await context.ExternalBus.SendToCellAsync(_matchingEngineCellId, order);
    }

    public Task OnStopAsync(ICellContext context)
    {
        context.Logger.LogInformation(
            "💼 [{Client}] Portfolio stopped. Cash: {Cash:C}, Positions: {Count}",
            _clientName, _cash, _positions.Count);

        foreach (var (symbol, pos) in _positions)
        {
            context.Logger.LogInformation(
                "📊 [{Client}] Final position: {Qty} {Symbol} @ {Avg:C}",
                _clientName, pos.Quantity, symbol, pos.AveragePrice);
        }

        return Task.CompletedTask;
    }
}