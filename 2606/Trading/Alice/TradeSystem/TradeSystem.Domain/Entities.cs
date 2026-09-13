// TradeSystem.Domain/Entities.cs
namespace TradeSystem.Domain;

public class Portfolio
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Ticker
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;     // SBER, GAZP, SRZ5
    public string SecId { get; set; } = string.Empty;      // биржевый идентификатор
    public string InstrumentType { get; set; } = string.Empty;  // Equity, Future
    public string Board { get; set; } = string.Empty;      // TQBR, RFUD
    public decimal LotSize { get; set; } = 1;
}

public class TradeStrategy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;  // MeanReversion, Momentum
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TickerStrategyBinding
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Guid TickerId { get; set; }
    public Guid StrategyId { get; set; }

    // Параметры связки
    public decimal CapitalAllocation { get; set; }
    public int MaxPosition { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Текущее состояние
    public int NetPosition { get; set; }
    public decimal AvgEntryPrice { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }

    // Навигация
    public Portfolio? Portfolio { get; set; }
    public Ticker? Ticker { get; set; }
    public TradeStrategy? Strategy { get; set; }
}

public class Position
{
    public Guid PortfolioId { get; set; }
    public Guid TickerId { get; set; }
    public int NetQty { get; set; }
    public decimal AvgPrice { get; set; }
}

public class StrategyPnL
{
    public Guid BindingId { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public DateTime UpdatedAt { get; set; }
}

