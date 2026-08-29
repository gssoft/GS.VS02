// 26.08.29
// StrategyProcessor.cs

using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public enum StrategyState
{
    Idle,               // нет позиции и нет активного ордера
    WaitingForFill,     // отправлен ордер, ждём исполнения/отказа
    InPosition          // есть открытая позиция (и нет активного ордера)
}

// Маркерный интерфейс для событий, обрабатываемых стратегией
public interface IStrategyEvent { }
public record NewQuotesStrategyEvent(NewQuotes Quotes) : IStrategyEvent;
public record OrderFilledStrategyEvent(OrderFilled Filled) : IStrategyEvent;
public record OrderNotFilledStrategyEvent(OrderNotFilled NotFilled) : IStrategyEvent;

public class StrategyProcessor : EventDrivenProcessor<IStrategyEvent>
{
    private readonly InMemoryDatabase _db;
    private readonly Dictionary<string, TickerState> _tickerStates = new();
    private readonly Dictionary<string, decimal> _localPositions = new(); // количество по тикеру

    private class TickerState
    {
        public StrategyState State { get; set; } = StrategyState.Idle;
        public string? PendingSide { get; set; } // "Buy" или "Sell"
    }

    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
        : base(bus, loggerFactory)
    {
        _db = db;

        // Подписываемся на внешние события, но только для перекладки в свой канал
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

            // Если уже есть активный ордер, пропускаем
            if (stateInfo.State == StrategyState.WaitingForFill)
                continue;

            if (stateInfo.State == StrategyState.Idle)
            {
                // Нет позиции – покупаем
                stateInfo.State = StrategyState.WaitingForFill;
                stateInfo.PendingSide = "Buy";
                var buyOrder = new OrderRequested(ticker, 10, quote.Ask, "Buy");
                await Bus.PublishAsync(buyOrder, ct);
                Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", ticker, quote.Ask);
            }
            else if (stateInfo.State == StrategyState.InPosition)
            {
                // Есть позиция – продаём всю позицию
                decimal quantity = _localPositions.GetValueOrDefault(ticker);
                stateInfo.State = StrategyState.WaitingForFill;
                stateInfo.PendingSide = "Sell";
                var sellOrder = new OrderRequested(ticker, quantity, quote.Bid, "Sell");
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
        // Если состояние не WaitingForFill, игнорируем
    }

    private void ProcessOrderNotFilled(OrderNotFilled notFilled)
    {
        string ticker = notFilled.Ticker;
        if (_tickerStates.TryGetValue(ticker, out var stateInfo))
        {
            if (stateInfo.State == StrategyState.WaitingForFill)
            {
                if (stateInfo.PendingSide == "Buy")
                    stateInfo.State = StrategyState.Idle;      // не купили
                else if (stateInfo.PendingSide == "Sell")
                    stateInfo.State = StrategyState.InPosition; // позиция осталась
                stateInfo.PendingSide = null;
            }
        }
    }
}

// 26.08.26
//StrategyProcessor.cs

//using Trading.Core;
//using Trading.Domain;
//using Trading.Storage;
//using Microsoft.Extensions.Logging;

//namespace Trading.Processors;

//public enum StrategyState
//{
//    Idle,               // нет позиции и нет активного ордера
//    WaitingForFill,     // отправлен ордер, ждём исполнения/отказа
//    InPosition          // есть открытая позиция (и нет активного ордера)
//}

//public class StrategyProcessor : EventDrivenProcessor<NewQuotes>
//{
//    private readonly InMemoryDatabase _db;
//    private readonly Dictionary<string, TickerState> _tickerStates = new();
//    private readonly object _stateLock = new();

//    // Локальное представление позиций для быстрого доступа (не зависит от PositionProcessor)
//    private readonly Dictionary<string, decimal> _localPositions = new();

//    private class TickerState
//    {
//        public StrategyState State { get; set; } = StrategyState.Idle;
//        public string? PendingSide { get; set; } // "Buy" или "Sell", если WaitingForFill
//    }

//    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
//        : base(bus, loggerFactory)
//    {
//        _db = db;

//        // Подписываемся на события исполнения ордеров
//        Bus.Subscribe<OrderFilled>((filled, ct) => OnOrderFilled(filled));
//        Bus.Subscribe<OrderNotFilled>((notFilled, ct) => OnOrderNotFilled(notFilled));
//    }

//    private Task OnOrderFilled(OrderFilled filled)
//    {
//        lock (_stateLock)
//        {
//            var ticker = filled.Ticker;
//            if (!_tickerStates.TryGetValue(ticker, out var stateInfo))
//                stateInfo = _tickerStates[ticker] = new TickerState();

//            // Если мы ожидали исполнения ордера
//            if (stateInfo.State == StrategyState.WaitingForFill)
//            {
//                if (stateInfo.PendingSide == "Buy")
//                {
//                    // Открываем позицию
//                    _localPositions[ticker] = _localPositions.GetValueOrDefault(ticker) + filled.Quantity;
//                    stateInfo.State = StrategyState.InPosition;
//                }
//                else if (stateInfo.PendingSide == "Sell")
//                {
//                    // Закрываем позицию
//                    _localPositions[ticker] = _localPositions.GetValueOrDefault(ticker) - filled.Quantity;
//                    stateInfo.State = StrategyState.Idle;
//                }
//                stateInfo.PendingSide = null;
//            }
//            // Если состояние не WaitingForFill, игнорируем (например, повторное исполнение или неожиданное)
//        }
//        return Task.CompletedTask;
//    }

//    private Task OnOrderNotFilled(OrderNotFilled notFilled)
//    {
//        lock (_stateLock)
//        {
//            var ticker = notFilled.Ticker;
//            if (_tickerStates.TryGetValue(ticker, out var stateInfo))
//            {
//                if (stateInfo.State == StrategyState.WaitingForFill)
//                {
//                    // Возвращаемся в предыдущее состояние в зависимости от стороны ордера
//                    if (stateInfo.PendingSide == "Buy")
//                    {
//                        stateInfo.State = StrategyState.Idle; // не купили, позиции нет
//                    }
//                    else if (stateInfo.PendingSide == "Sell")
//                    {
//                        stateInfo.State = StrategyState.InPosition; // позиция осталась
//                    }
//                    stateInfo.PendingSide = null;
//                }
//            }
//        }
//        return Task.CompletedTask;
//    }

//    protected override async Task HandleAsync(NewQuotes message, CancellationToken ct)
//    {
//        foreach (var quote in message.Quotes)
//        {
//            string ticker = quote.Ticker;
//            StrategyState currentState;
//            string? pendingSide;

//            lock (_stateLock)
//            {
//                if (!_tickerStates.TryGetValue(ticker, out var stateInfo))
//                    stateInfo = _tickerStates[ticker] = new TickerState();

//                currentState = stateInfo.State;
//                pendingSide = stateInfo.PendingSide;

//                // Если уже есть активный ордер, пропускаем
//                if (currentState == StrategyState.WaitingForFill)
//                    continue;

//                // Принимаем решение о новом ордере
//                if (currentState == StrategyState.Idle)
//                {
//                    // Нет позиции – покупаем
//                    stateInfo.State = StrategyState.WaitingForFill;
//                    stateInfo.PendingSide = "Buy";
//                }
//                else if (currentState == StrategyState.InPosition)
//                {
//                    // Есть позиция – продаём
//                    stateInfo.State = StrategyState.WaitingForFill;
//                    stateInfo.PendingSide = "Sell";
//                }
//            }

//            // Отправляем ордер (вне блокировки)
//            if (pendingSide == null) // фактически всегда не null после изменения, но для ясности
//            {
//                if (currentState == StrategyState.Idle)
//                {
//                    var buyOrder = new OrderRequested(ticker, 10, quote.Ask, "Buy");
//                    await Bus.PublishAsync(buyOrder, ct);
//                    Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", ticker, quote.Ask);
//                }
//                else if (currentState == StrategyState.InPosition)
//                {
//                    decimal quantity = _localPositions.GetValueOrDefault(ticker);
//                    var sellOrder = new OrderRequested(ticker, quantity, quote.Bid, "Sell");
//                    await Bus.PublishAsync(sellOrder, ct);
//                    Logger.LogInformation("Strategy: Sending SELL order for {Ticker} at {Price}", ticker, quote.Bid);
//                }
//            }
//        }
//    }
//}

//// StrategyProcessor.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Storage;
//using Microsoft.Extensions.Logging;

//namespace Trading.Processors;

//public class StrategyProcessor : EventDrivenProcessor<NewQuotes>
//{
//    private readonly InMemoryDatabase _db;
//    private readonly HashSet<string> _pendingTickers = new();
//    private readonly object _lock = new();

//    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
//        : base(bus, loggerFactory)
//    {
//        _db = db;

//        // Подписываемся на события исполнения ордеров, чтобы снимать блокировку
//        Bus.Subscribe<OrderFilled>((filled, ct) => OnOrderFilled(filled));
//        Bus.Subscribe<OrderNotFilled>((notFilled, ct) => OnOrderNotFilled(notFilled));
//    }

//    private Task OnOrderFilled(OrderFilled filled)
//    {
//        RemovePending(filled.Ticker);
//        return Task.CompletedTask;
//    }

//    private Task OnOrderNotFilled(OrderNotFilled notFilled)
//    {
//        // Ищем тикер по OrderId в БД
//        if (_db.Orders.TryGetValue(notFilled.OrderId, out var order))
//            RemovePending(order.Ticker);
//        return Task.CompletedTask;
//    }

//    private void AddPending(string ticker)
//    {
//        lock (_lock) { _pendingTickers.Add(ticker); }
//    }

//    private void RemovePending(string ticker)
//    {
//        lock (_lock) { _pendingTickers.Remove(ticker); }
//    }

//    private bool IsPending(string ticker)
//    {
//        lock (_lock) { return _pendingTickers.Contains(ticker); }
//    }

//    protected override async Task HandleAsync(NewQuotes message, CancellationToken ct)
//    {
//        foreach (var quote in message.Quotes)
//        {
//            string ticker = quote.Ticker;

//            // Если по тикеру уже есть неисполненный ордер — пропускаем
//            if (IsPending(ticker))
//                continue;

//            bool hasPosition = _db.Positions.TryGetValue(ticker, out var pos) && pos.Quantity != 0;

//            if (!hasPosition)
//            {
//                var order = new OrderRequested(ticker, 10, quote.Ask, "Buy");
//                AddPending(ticker); // блокируем до получения ответа
//                await Bus.PublishAsync(order, ct);
//                Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", ticker, quote.Ask);
//            }
//            else
//            {
//                var order = new OrderRequested(ticker, pos.Quantity, quote.Bid, "Sell");
//                AddPending(ticker);
//                await Bus.PublishAsync(order, ct);
//                Logger.LogInformation("Strategy: Sending SELL order for {Ticker} at {Price}", ticker, quote.Bid);
//            }
//        }
//    }
//}

//// StrategyProcessor.cs
//using Trading.Core;
//using Trading.Domain;
//using Trading.Storage;
//using Microsoft.Extensions.Logging;

//namespace Trading.Processors;

//public class StrategyProcessor : EventDrivenProcessor<NewQuotes>
//{
//    private readonly InMemoryDatabase _db;
//    private readonly Random _rnd = new();

//    public StrategyProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
//        : base(bus, loggerFactory)
//    {
//        _db = db;
//    }

//    protected override async Task HandleAsync(NewQuotes message, CancellationToken ct)
//    {
//        foreach (var quote in message.Quotes)
//        {
//            // Простая стратегия: если нет позиции – покупаем, если есть – продаём (для примера)
//            bool hasPosition = _db.Positions.TryGetValue(quote.Ticker, out var pos) && pos.Quantity != 0;

//            if (!hasPosition)
//            {
//                // Открываем позицию
//                var order = new OrderRequested(
//                    Ticker: quote.Ticker,
//                    Quantity: 10,
//                    Price: quote.Ask,
//                    Side: "Buy");

//                await Bus.PublishAsync(order, ct);
//                Logger.LogInformation("Strategy: Sending BUY order for {Ticker} at {Price}", quote.Ticker, quote.Ask);
//            }
//            else
//            {
//                // Закрываем позицию

//                var order = new OrderRequested(
//                    Ticker: quote.Ticker,
//                    Quantity: pos.Quantity,
//                    Price: quote.Bid,
//                    Side: "Sell");

//                await Bus.PublishAsync(order, ct);
//                Logger.LogInformation("Strategy: Sending SELL order for {Ticker} at {Price}", quote.Ticker, quote.Bid);
//            }
//        }
//    }
//}
