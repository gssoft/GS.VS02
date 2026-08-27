# Trading System - Детальное распределение сервисов
## Полная архитектура с .NET Aspire

---

## 📦 SOLUTION STRUCTURE

```
TradingSystem.sln
├── src/
│   ├── TradingSystem.AppHost/                    # .NET Aspire AppHost
│   ├── TradingSystem.ServiceDefaults/            # Shared defaults
│   │
│   ├── Core/                                     # Shared libraries
│   │   ├── TradingSystem.Core.Models/
│   │   ├── TradingSystem.Core.MessageBus/
│   │   └── TradingSystem.Core.Contracts/
│   │
│   ├── Services/                                 # Microservices
│   │   ├── MarketData.Generator/
│   │   ├── MarketData.Storage/
│   │   ├── Exchange.Connector/
│   │   ├── Portfolio.Manager/
│   │   ├── Strategy.Executor/
│   │   ├── Database.InMemory/
│   │   └── Database.Persistent/
│   │
│   └── Gateway/
│       └── TradingSystem.ApiGateway/
│
└── tests/
    └── TradingSystem.Tests/
```

---

## 🎯 PROJECT 1: TradingSystem.Core.Models
**Тип**: Class Library
**Назначение**: Общие модели данных для всей системы

### Модели:

```csharp
// Market Data
public class Quote
{
    public DateTime TimeStamp { get; set; }
    public string TradeBoard { get; set; }
    public string Ticker { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Last { get; set; }
    public long Volume { get; set; }
    public long OpenInterest { get; set; }
}

// Orders
public enum OrderType { Market, Limit, Stop, StopLimit }
public enum OrderSide { Buy, Sell }
public enum OrderStatus { New, PartiallyFilled, Filled, Cancelled, Rejected }

public class Order
{
    public Guid OrderId { get; set; }
    public Guid? StrategyId { get; set; }
    public string Ticker { get; set; }
    public OrderType Type { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

// Trades
public class Trade
{
    public Guid TradeId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? StrategyId { get; set; }
    public string Ticker { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public DateTime TimeStamp { get; set; }
}

// Portfolio
public class Portfolio
{
    public Guid PortfolioId { get; set; }
    public string Name { get; set; }
    public List<string> Tickers { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CurrentCapital { get; set; }
}

// Position (FIFO based)
public class Position
{
    public Guid PositionId { get; set; }
    public Guid PortfolioId { get; set; }
    public string Ticker { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
}

// Deal (from FIFO)
public class Deal
{
    public Guid DealId { get; set; }
    public Guid PositionId { get; set; }
    public Trade EntryTrade { get; set; }
    public Trade ExitTrade { get; set; }
    public decimal Quantity { get; set; }
    public decimal PnL { get; set; }
    public TimeSpan Duration { get; set; }
}

// Equity
public class Equity
{
    public Guid EquityId { get; set; }
    public Guid PortfolioId { get; set; }
    public DateTime TimeStamp { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Cash { get; set; }
    public decimal PositionsValue { get; set; }
    public decimal DailyPnL { get; set; }
}

// Strategy
public class Strategy
{
    public Guid StrategyId { get; set; }
    public string Name { get; set; }
    public string Ticker { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public bool IsActive { get; set; }
}

// Strategy Portfolio (Гельфанд)
public class StrategyPortfolio
{
    public Guid PortfolioId { get; set; }
    public string Name { get; set; }
    public List<StrategyAllocation> Strategies { get; set; }
}

public class StrategyAllocation
{
    public Guid StrategyId { get; set; }
    public decimal AllocationWeight { get; set; }
}

// Signal
public enum SignalAction { Buy, Sell, Hold, Close }

public class Signal
{
    public Guid SignalId { get; set; }
    public Guid StrategyId { get; set; }
    public string Ticker { get; set; }
    public SignalAction Action { get; set; }
    public decimal Strength { get; set; }
    public DateTime TimeStamp { get; set; }
}
```

---

## 🎯 PROJECT 2: TradingSystem.Core.MessageBus
**Тип**: Class Library
**Назначение**: Универсальный транспорт для всех типов взаимодействия

### Интерфейсы:

```csharp
public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default);
}

public interface IMessageBusFactory
{
    IMessageBus CreateInProcess();           // Channels
    IMessageBus CreateNamedPipes(string name); // Named Pipes
    IMessageBus CreateTcp(string host, int port); // TCP
}
```

### Реализации:

```csharp
// 1. In-Process (Channels)
public class ChannelMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<object>> _channels;
}

// 2. Named Pipes (IPC)
public class NamedPipeMessageBus : IMessageBus
{
    private readonly string _pipeName;
}

// 3. TCP (Network)
public class TcpMessageBus : IMessageBus
{
    private readonly string _host;
    private readonly int _port;
}
```

---

## 🎯 PROJECT 3: TradingSystem.Core.Contracts
**Тип**: Class Library
**Назначение**: Контракты событий и команд

```csharp
// Events
public record QuotePublished(Quote Quote);
public record OrderCreated(Order Order);
public record OrderExecuted(Trade Trade);
public record PositionUpdated(Position Position);
public record SignalGenerated(Signal Signal);

// Commands
public record CreateOrderCommand(Order Order);
public record CancelOrderCommand(Guid OrderId);
public record UpdateStrategyCommand(Strategy Strategy);
```

---

## 🎯 PROJECT 4: MarketData.Generator
**Тип**: ASP.NET Core Web API
**Назначение**: Генерация и распределение котировок

### BackgroundServices:

#### 1. QuoteGeneratorService
```csharp
public class QuoteGeneratorService : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly string[] _tickers = { "SBER", "GAZP", "LKOH" };
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var ticker in _tickers)
            {
                var quote = GenerateQuote(ticker);
                await _messageBus.PublishAsync("quotes", quote, ct);
            }
            await Task.Delay(1000, ct); // 1 quote/sec per ticker
        }
    }
}
```

#### 2. QuoteDistributorHubService
```csharp
public class QuoteDistributorHubService : BackgroundService
{
    private readonly IMessageBus _internalBus;  // Channels
    private readonly IMessageBus _namedPipeBus; // Named Pipes
    private readonly IMessageBus _tcpBus;       // TCP
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Подписка на внутренний канал
        await _internalBus.SubscribeAsync<Quote>("quotes", async quote =>
        {
            // Распределение через Named Pipes
            await _namedPipeBus.PublishAsync("quotes", quote, ct);
            
            // Распределение через TCP (если есть удаленные подписчики)
            await _tcpBus.PublishAsync("quotes", quote, ct);
        }, ct);
    }
}
```

### Endpoints:
- `GET /api/quotes/{ticker}` - последняя котировка
- `GET /api/quotes/{ticker}/stream` - SSE stream котировок

---

## 🎯 PROJECT 5: MarketData.Storage
**Тип**: ASP.NET Core Web API
**Назначание**: Хранение и предоставление котировок

### BackgroundServices:

#### 1. QuoteReceiverService
```csharp
public class QuoteReceiverService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<Quote>("quotes", async quote =>
        {
            // Передача внутрь для обработки
            await _internalBus.PublishAsync("quotes.received", quote, ct);
        }, ct);
    }
}
```

#### 2. QuotePersistenceService
```csharp
public class QuotePersistenceService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IQuoteRepository _repository;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Quote>("quotes.received", async quote =>
        {
            await _repository.SaveQuoteAsync(quote, ct);
        }, ct);
    }
}
```

#### 3. QuoteCacheService
```csharp
public class QuoteCacheService : BackgroundService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromDays(2);
    
    // Кэширует последние 2-3 дня в памяти
}
```

#### 4. QuoteQueryService
```csharp
public class QuoteQueryService : BackgroundService
{
    // Обрабатывает запросы на получение исторических данных
}
```

### Endpoints:
- `GET /api/quotes/history/{ticker}?from={date}&to={date}`
- `GET /api/quotes/latest/{ticker}`
- `GET /api/quotes/test-data/{ticker}` - тестовые данные для бэктестинга

---

## 🎯 PROJECT 6: Exchange.Connector
**Тип**: ASP.NET Core Web API
**Назначение**: Взаимодействие с биржей

### BackgroundServices:

#### 1. OrderReceiverService
```csharp
public class OrderReceiverService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<Order>("orders", async order =>
        {
            await _internalBus.PublishAsync("orders.received", order, ct);
        }, ct);
    }
}
```

#### 2. OrderSenderService
```csharp
public class OrderSenderService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IExchangeClient _exchangeClient;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Order>("orders.received", async order =>
        {
            // Отправка на биржу
            var result = await _exchangeClient.SendOrderAsync(order, ct);
            
            // Публикация результата
            await _internalBus.PublishAsync("orders.sent", result, ct);
        }, ct);
    }
}
```

#### 3. TradeReceiverService
```csharp
public class TradeReceiverService : BackgroundService
{
    private readonly IExchangeClient _exchangeClient;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Подписка на WebSocket биржи для получения исполнений
        await _exchangeClient.SubscribeToTradesAsync(async trade =>
        {
            await _internalBus.PublishAsync("trades.received", trade, ct);
        }, ct);
    }
}
```

#### 4. ExchangeEventHubService
```csharp
public class ExchangeEventHubService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IMessageBus _namedPipeBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Распределение trades другим сервисам
        await _internalBus.SubscribeAsync<Trade>("trades.received", async trade =>
        {
            await _namedPipeBus.PublishAsync("trades", trade, ct);
        }, ct);
    }
}
```

### Endpoints:
- `POST /api/orders` - создание ордера
- `DELETE /api/orders/{orderId}` - отмена ордера
- `GET /api/orders/{orderId}` - статус ордера

---

## 🎯 PROJECT 7: Portfolio.Manager
**Тип**: ASP.NET Core Web API
**Назначение**: Управление портфелями

### BackgroundServices:

#### 1. QuoteSubscriberService
```csharp
public class QuoteSubscriberService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<Quote>("quotes", async quote =>
        {
            await _internalBus.PublishAsync("quotes.portfolio", quote, ct);
        }, ct);
    }
}
```

#### 2. TradeSubscriberService
```csharp
public class TradeSubscriberService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<Trade>("trades", async trade =>
        {
            await _internalBus.PublishAsync("trades.portfolio", trade, ct);
        }, ct);
    }
}
```

#### 3. PositionCalculatorService (FIFO)
```csharp
public class PositionCalculatorService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IPositionRepository _repository;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Trade>("trades.portfolio", async trade =>
        {
            // FIFO алгоритм
            var position = await CalculatePositionFifoAsync(trade, ct);
            await _repository.UpdatePositionAsync(position, ct);
            
            // Публикация обновления
            await _internalBus.PublishAsync("positions.updated", position, ct);
        }, ct);
    }
    
    private async Task<Position> CalculatePositionFifoAsync(Trade trade, CancellationToken ct)
    {
        // Реализация FIFO
        // 1. Получить текущую позицию
        // 2. Если Buy - добавить в очередь
        // 3. Если Sell - закрыть из начала очереди (FIFO)
        // 4. Рассчитать Average Price, PnL
    }
}
```

#### 4. DealCalculatorService
```csharp
public class DealCalculatorService : BackgroundService
{
    // Создает Deal при закрытии позиции через FIFO
}
```

#### 5. EquityCalculatorService
```csharp
public class EquityCalculatorService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Каждые 5 минут пересчитываем equity
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            
            var equity = await CalculateEquityAsync(ct);
            await _internalBus.PublishAsync("equity.calculated", equity, ct);
        }
    }
}
```

#### 6. PortfolioManagerService
```csharp
public class PortfolioManagerService : BackgroundService
{
    // Управление портфелями, создание, удаление, ребалансировка
}
```

### Endpoints:
- `GET /api/portfolios`
- `GET /api/portfolios/{id}/positions`
- `GET /api/portfolios/{id}/equity`
- `GET /api/portfolios/{id}/deals`

---

## 🎯 PROJECT 8: Strategy.Executor
**Тип**: ASP.NET Core Web API
**Назначение**: Выполнение торговых стратегий

### BackgroundServices:

#### 1. QuoteSubscriberService
```csharp
public class QuoteSubscriberService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<Quote>("quotes", async quote =>
        {
            await _internalBus.PublishAsync("quotes.strategy", quote, ct);
        }, ct);
    }
}
```

#### 2. StrategyExecutorService
```csharp
public class StrategyExecutorService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IStrategyRepository _repository;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Quote>("quotes.strategy", async quote =>
        {
            // Получить все активные стратегии для этого тикера
            var strategies = await _repository.GetActiveStrategiesAsync(quote.Ticker, ct);
            
            foreach (var strategy in strategies)
            {
                // Выполнить стратегию
                await ExecuteStrategyAsync(strategy, quote, ct);
            }
        }, ct);
    }
}
```

#### 3. SignalGeneratorService
```csharp
public class SignalGeneratorService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<(Strategy, Quote)>("strategy.executed", async data =>
        {
            var signal = await GenerateSignalAsync(data.Item1, data.Item2, ct);
            
            if (signal != null)
            {
                await _internalBus.PublishAsync("signals.generated", signal, ct);
            }
        }, ct);
    }
}
```

#### 4. RiskManagerService
```csharp
public class RiskManagerService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Signal>("signals.generated", async signal =>
        {
            // Проверка рисков
            var approved = await CheckRiskLimitsAsync(signal, ct);
            
            if (approved)
            {
                await _internalBus.PublishAsync("signals.approved", signal, ct);
            }
        }, ct);
    }
}
```

#### 5. OrderGeneratorService
```csharp
public class OrderGeneratorService : BackgroundService
{
    private readonly IMessageBus _internalBus;
    private readonly IMessageBus _namedPipeBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _internalBus.SubscribeAsync<Signal>("signals.approved", async signal =>
        {
            var order = await CreateOrderFromSignalAsync(signal, ct);
            
            // Отправка ордера в Exchange.Connector
            await _namedPipeBus.PublishAsync("orders", order, ct);
        }, ct);
    }
}
```

#### 6. StrategyPortfolioManagerService (Гельфанд)
```csharp
public class StrategyPortfolioManagerService : BackgroundService
{
    // Управление портфелями стратегий
    // Аллокация капитала между стратегиями
}
```

### Endpoints:
- `GET /api/strategies`
- `POST /api/strategies`
- `PUT /api/strategies/{id}`
- `GET /api/strategies/{id}/signals`
- `GET /api/strategy-portfolios`

---

## 🎯 PROJECT 9: Database.InMemory
**Тип**: ASP.NET Core Web API
**Назначение**: Оперативное хранилище (24 часа)

### BackgroundServices:

#### 1. DataReceiverService
```csharp
public class DataReceiverService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IMemoryCache _cache;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Подписка на все типы данных
        await _namedPipeBus.SubscribeAsync<Quote>("quotes", SaveToCache, ct);
        await _namedPipeBus.SubscribeAsync<Order>("orders", SaveToCache, ct);
        await _namedPipeBus.SubscribeAsync<Trade>("trades", SaveToCache, ct);
        await _namedPipeBus.SubscribeAsync<Position>("positions", SaveToCache, ct);
        await _namedPipeBus.SubscribeAsync<Signal>("signals", SaveToCache, ct);
    }
}
```

#### 2. QueryService
```csharp
public class QueryService : BackgroundService
{
    // Обработка запросов на чтение из кэша
}
```

#### 3. DataEvictionService
```csharp
public class DataEvictionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Каждый час удаляем данные старше 24 часов
            await Task.Delay(TimeSpan.FromHours(1), ct);
            await EvictOldDataAsync(ct);
        }
    }
}
```

#### 4. DataSyncService
```csharp
public class DataSyncService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Каждые 5 минут отправляем данные в персистентное хранилище
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            
            var batch = await GetBatchForSyncAsync(ct);
            await _namedPipeBus.PublishAsync("data.sync", batch, ct);
        }
    }
}
```

### Storage:
- Redis для быстрого доступа
- In-Memory Dictionary для горячих данных

---

## 🎯 PROJECT 10: Database.Persistent
**Тип**: ASP.NET Core Web API
**Назначение**: Долговременное хранилище

### BackgroundServices:

#### 1. DataWriterService
```csharp
public class DataWriterService : BackgroundService
{
    private readonly IMessageBus _namedPipeBus;
    private readonly IDbContext _dbContext;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _namedPipeBus.SubscribeAsync<object>("data.sync", async batch =>
        {
            await _dbContext.BulkInsertAsync(batch, ct);
        }, ct);
    }
}
```

#### 2. QueryService
```csharp
public class QueryService : BackgroundService
{
    // Обработка сложных аналитических запросов
}
```

#### 3. ArchiveService
```csharp
public class ArchiveService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Раз в день архивируем старые данные
            await Task.Delay(TimeSpan.FromDays(1), ct);
            await ArchiveOldDataAsync(ct);
        }
    }
}
```

#### 4. BackupService
```csharp
public class BackupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Каждые 6 часов создаем бэкап
            await Task.Delay(TimeSpan.FromHours(6), ct);
            await CreateBackupAsync(ct);
        }
    }
}
```

### Database:
- PostgreSQL с TimescaleDB для временных рядов
- Partitioning по дате

---

## 📋 SUMMARY: Распределение по проектам

| Проект | BackgroundServices | Сущности | Транспорт In | Транспорт Out |
|--------|-------------------|----------|--------------|---------------|
| **MarketData.Generator** | QuoteGenerator, QuoteDistributorHub | Quote | - | Named Pipes, TCP |
| **MarketData.Storage** | QuoteReceiver, QuotePersistence, QuoteCache, QuoteQuery | Quote (stored) | Named Pipes | HTTP API |
| **Exchange.Connector** | OrderReceiver, OrderSender, TradeReceiver, ExchangeEventHub | Order, Trade | Named Pipes | WebSocket, Named Pipes |
| **Portfolio.Manager** | QuoteSubscriber, TradeSubscriber, PositionCalculator, DealCalculator, EquityCalculator, PortfolioManager | Portfolio, Position, Deal, Equity | Named Pipes | Named Pipes, HTTP API |
| **Strategy.Executor** | QuoteSubscriber, StrategyExecutor, SignalGenerator, RiskManager, OrderGenerator, StrategyPortfolioManager | Strategy, StrategyPortfolio, Signal | Named Pipes | Named Pipes, HTTP API |
| **Database.InMemory** | DataReceiver, Query, DataEviction, DataSync | All (cached 24h) | Named Pipes | Named Pipes, HTTP API |
| **Database.Persistent** | DataWriter, Query, Archive, Backup | All (permanent) | Named Pipes | HTTP API |

**Всего BackgroundServices: 30+**
**Всего проектов: 10 (7 сервисов + 3 библиотеки)**

Готов к следующему шагу: создание .NET Aspire AppHost! 🚀
