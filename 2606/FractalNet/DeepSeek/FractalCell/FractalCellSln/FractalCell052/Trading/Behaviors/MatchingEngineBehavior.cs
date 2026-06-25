// Шаг 7: Поведение — Исполнитель Ордеров (Matching Engine)
// Создайте файл Trading / Behaviors / MatchingEngineBehavior.cs:

using FractalCell02.Trading.Behaviors;
using FractalCell02.Core.Behaviors;
using FractalCell02.Core.Interfaces;
using FractalCell02.Trading.Events;
using Microsoft.Extensions.Logging;

namespace FractalCell02.Trading.Behaviors;

/// <summary>
/// Движок исполнения ордеров: хранит стакан заявок,
/// при каждой новой котировке проверяет пересечение,
/// генерирует TradeEvent и отправляет его портфелю клиента
/// </summary>
public class MatchingEngineBehavior : ICellBehavior
{
    private readonly string _symbol;
    private readonly List<OrderEvent> _pendingOrders = new();
    private readonly object _lock = new();

    private decimal _lastBid;
    private decimal _lastAsk;
    private int _tradeCount;

    public MatchingEngineBehavior(string symbol)
    {
        _symbol = symbol;
    }

    public Task OnStartAsync(ICellContext context)
    {
        context.Logger.LogInformation(
            "⚙️ MatchingEngine started for {Symbol}", _symbol);
        return Task.CompletedTask;
    }

    public async Task OnMessageAsync(IApplicationEvent @event, ICellContext context)
    {
        switch (@event)
        {
            case QuoteEvent quote when quote.Symbol == _symbol:
                await HandleQuote(quote, context);
                break;

            case OrderEvent order when order.Symbol == _symbol:
                await HandleOrder(order, context);
                break;
        }
    }

    private async Task HandleQuote(QuoteEvent quote, ICellContext context)
    {
        _lastBid = quote.Bid;
        _lastAsk = quote.Ask;

        context.Logger.LogDebug(
            "⚙️ [ME] New quote: Bid={Bid} Ask={Ask}, pending orders: {Count}",
            _lastBid, _lastAsk, _pendingOrders.Count);

        // Проверяем лимитные ордера на исполнение
        List<OrderEvent> filled;
        lock (_lock)
        {
            filled = _pendingOrders.Where(o => IsExecutable(o)).ToList();
            foreach (var order in filled)
            {
                _pendingOrders.Remove(order);
            }
        }

        foreach (var order in filled)
        {
            await ExecuteTrade(order, context);
        }
    }

    private async Task HandleOrder(OrderEvent order, ICellContext context)
    {
        context.Logger.LogInformation(
            "📝 [ME] New order: {Side} {Qty} {Symbol} @ {Price} ({Type}) from {Client}",
            order.Side, order.Quantity, order.Symbol, order.Price,
            order.Type, order.ClientCellId);

        if (order.Type == OrderType.Market)
        {
            // Маркет-ордер исполняется сразу по текущей цене
            var execPrice = order.Side == OrderSide.Buy ? _lastAsk : _lastBid;
            if (execPrice > 0)
            {
                await ExecuteTrade(order, context, execPrice);
            }
            else
            {
                context.Logger.LogWarning(
                    "⚠️ [ME] Cannot execute market order — no quotes yet");
            }
        }
        else
        {
            // Лимитный ордер — проверяем немедленное исполнение
            if (IsExecutable(order))
            {
                await ExecuteTrade(order, context);
            }
            else
            {
                lock (_lock)
                {
                    _pendingOrders.Add(order);
                }
                context.Logger.LogInformation(
                    "📋 [ME] Order {OrderId} added to book. Pending: {Count}",
                    order.OrderId, _pendingOrders.Count);
            }
        }
    }

    private bool IsExecutable(OrderEvent order)
    {
        if (_lastBid == 0 && _lastAsk == 0) return false;

        return order.Side switch
        {
            // Buy: исполняется если Ask <= лимитная цена
            OrderSide.Buy => _lastAsk <= order.Price,
            // Sell: исполняется если Bid >= лимитная цена
            OrderSide.Sell => _lastBid >= order.Price,
            _ => false
        };
    }

    private async Task ExecuteTrade(OrderEvent order, ICellContext context, decimal? overridePrice = null)
    {
        var execPrice = overridePrice ?? order.Price;
        _tradeCount++;

        var trade = new TradeEvent(
            EventId: $"trade-{Guid.NewGuid():N}"[..16],
            Timestamp: DateTime.UtcNow,
            TradeId: $"T{_tradeCount:D5}",
            OrderId: order.OrderId,
            ClientCellId: order.ClientCellId,
            Symbol: order.Symbol,
            ExecutedPrice: execPrice,
            Quantity: order.Quantity,
            Side: order.Side,
            SourceCellId: context.CellId);

        context.Logger.LogInformation(
            "✅ [ME] TRADE EXECUTED: {TradeId} | {Side} {Qty} {Symbol} @ {Price} → {Client}",
            trade.TradeId, trade.Side, trade.Quantity, trade.Symbol,
            trade.ExecutedPrice, trade.ClientCellId);

        // Отправляем Trade конкретному портфелю клиента
        await context.ExternalBus.SendToCellAsync(order.ClientCellId, trade);
    }

    public Task OnStopAsync(ICellContext context)
    {
        context.Logger.LogInformation(
            "⚙️ MatchingEngine stopped. Total trades: {Count}, Pending: {Pending}",
            _tradeCount, _pendingOrders.Count);
        return Task.CompletedTask;
    }
}
