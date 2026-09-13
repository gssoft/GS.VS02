// TradeSystem.Contracts/Dtos.cs
namespace TradeSystem.Contracts;

public record StrategyRegistrationRequest(
    Guid PortfolioId,
    Guid TickerId,
    string StrategyType,
    Dictionary<string, object> Parameters,
    decimal CapitalAllocation,
    int MaxPosition
);

public record StrategyRegisteredEvent(
    Guid BindingId,
    Guid PortfolioId,
    Guid TickerId,
    Guid StrategyId,
    DateTime RegisteredAt
);

public record TickerQuote(
    string Symbol,
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTime Timestamp
);

public record SignalDto(
    Guid BindingId,
    string Symbol,
    string Side,       // Buy, Sell
    int Quantity,
    decimal? Price,    // null = market order
    DateTime Timestamp
);

public record OrderDto(
    Guid Id,
    Guid BindingId,
    string Symbol,
    string Side,
    int Quantity,
    decimal? Price,
    string Status,    // Pending, Filled, Rejected, Cancelled
    DateTime CreatedAt
);

public record TradeDto(
    Guid Id,
    Guid OrderId,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    DateTime ExecutedAt
);

public record PositionSnapshot(
    Guid BindingId,
    string Symbol,
    string StrategyName,
    int NetPosition,
    decimal AvgEntryPrice,
    decimal RealizedPnL,
    decimal UnrealizedPnL
);

public record PortfolioSnapshot(
    Guid PortfolioId,
    string PortfolioName,
    decimal TotalRealizedPnL,
    decimal TotalUnrealizedPnL,
    List<PositionSnapshot> Positions
);

