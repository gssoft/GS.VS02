// 26.08.30

using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public enum StrategyState
{
    Idle,
    WaitingForFill,
    InPosition
}

public interface IStrategyEvent { }
public record NewQuotesStrategyEvent(NewQuotes Quotes) : IStrategyEvent;
public record OrderFilledStrategyEvent(OrderFilled Filled) : IStrategyEvent;
public record OrderNotFilledStrategyEvent(OrderNotFilled NotFilled) : IStrategyEvent;

public class StrategyProcessor : ProcessorBase<IStrategyEvent>
{
    private readonly InMemoryDatabase _db;

    private readonly decimal _lotSize;
    private readonly Dictionary<string, TickerState> _tickerStates = new();
    private readonly Dictionary<string, decimal> _localPositions = new();

    private class TickerState
    {
        public StrategyState State { get; set; } = StrategyState.Idle;
        public string? PendingSide { get; set; }
    }

    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db, decimal lotSize = 10m)
        : base(bus, loggerFactory)
    {
        _db = db;
        _lotSize = lotSize;

        Bus.Subscribe<NewQuotes>((quotes, ct) => EnqueueAsync(new NewQuotesStrategyEvent(quotes), ct));
        Bus.Subscribe<OrderFilled>((filled, ct) => EnqueueAsync(new OrderFilledStrategyEvent(filled), ct));
        Bus.Subscribe<OrderNotFilled>((notFilled, ct) => EnqueueAsync(new OrderNotFilledStrategyEvent(notFilled), ct));
    }

    protected override async Task HandleAsync(IStrategyEvent message, CancellationToken ct)
    {
        switch (message)
        {
            case NewQuotesStrategyEvent newQuotesEvent:
                await ProcessNewQuotesAsync(newQuotesEvent.Quotes, ct);
                break;
            case OrderFilledStrategyEvent filledEvent:
                ProcessOrderFilled(filledEvent.Filled);
                break;
            case OrderNotFilledStrategyEvent notFilledEvent:
                ProcessOrderNotFilled(notFilledEvent.NotFilled);
                break;
        }
    }

    private async Task ProcessNewQuotesAsync(NewQuotes message, CancellationToken ct)
    {
        foreach (var quote in message.Quotes)
        {
            string ticker = quote.Ticker;
            if (!_tickerStates.TryGetValue(ticker, out var stateInfo))
                stateInfo = _tickerStates[ticker] = new TickerState();

            if (stateInfo.State == StrategyState.WaitingForFill)
                continue;

            if (stateInfo.State == StrategyState.Idle)
            {
                stateInfo.State = StrategyState.WaitingForFill;
                stateInfo.PendingSide = "Buy";
                // var buyOrder = new OrderRequested(ticker, _lotSize, quote.Ask, "Buy");
                var buyOrder = new OrderRequested { Ticker = ticker, Quantity = _lotSize, Price = quote.Ask, Side = "Buy" };
                await Bus.PublishAsync(buyOrder, ct);
                Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", ticker, quote.Ask);
            }
            else if (stateInfo.State == StrategyState.InPosition)
            {
                decimal quantity = _localPositions.GetValueOrDefault(ticker);
                stateInfo.State = StrategyState.WaitingForFill;
                stateInfo.PendingSide = "Sell";
                // var sellOrder = new OrderRequested(ticker, quantity, quote.Bid, "Sell");
                var sellOrder = new OrderRequested { Ticker = ticker, Quantity = quantity, Price = quote.Bid, Side = "Sell" };
                await Bus.PublishAsync(sellOrder, ct);
                Logger.LogInformation("Strategy: Sending SELL order for {Ticker} at {Price}", ticker, quote.Bid);
            }
        }
    }

    private void ProcessOrderFilled(OrderFilled filled)
    {
        string ticker = filled.Ticker;
        if (!_tickerStates.TryGetValue(ticker, out var stateInfo))
            stateInfo = _tickerStates[ticker] = new TickerState();

        if (stateInfo.State == StrategyState.WaitingForFill)
        {
            if (stateInfo.PendingSide == "Buy")
            {
                _localPositions[ticker] = _localPositions.GetValueOrDefault(ticker) + filled.Quantity;
                stateInfo.State = StrategyState.InPosition;
            }
            else if (stateInfo.PendingSide == "Sell")
            {
                _localPositions[ticker] = _localPositions.GetValueOrDefault(ticker) - filled.Quantity;
                stateInfo.State = StrategyState.Idle;
            }
            stateInfo.PendingSide = null;
        }
    }

    private void ProcessOrderNotFilled(OrderNotFilled notFilled)
    {
        string ticker = notFilled.Ticker;
        if (_tickerStates.TryGetValue(ticker, out var stateInfo))
        {
            if (stateInfo.State == StrategyState.WaitingForFill)
            {
                if (stateInfo.PendingSide == "Buy")
                    stateInfo.State = StrategyState.Idle;
                else if (stateInfo.PendingSide == "Sell")
                    stateInfo.State = StrategyState.InPosition;
                stateInfo.PendingSide = null;
            }
        }
    }
}