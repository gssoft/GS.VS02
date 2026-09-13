# Trading Domain Model (C# .NET Aspire)

## Core Entities
- **Portfolio**: logical container for trading bindings; manages capital and risk limits.
- **Ticker**: tradable instrument (equity/future); no direct link to strategies.
- **TradeStrategy**: algorithmic strategy; instrument-agnostic; reusable across tickers.
- **TickerStrategyBinding**: atomic trading unit (Portfolio, Ticker, Strategy). This is what the portfolio "trades".

## Relationships
- Portfolio → 1:N → TickerStrategyBinding
- TickerStrategyBinding → N:1 → Ticker
- TickerStrategyBinding → N:1 → TradeStrategy
- Ticker ↔ TradeStrategy: Many-to-Many via TickerStrategyBinding.

## Accounting Rules
- P&L is attributed to TickerStrategyBinding (not to Ticker or Strategy alone).
- Position per Ticker is an aggregate (sum of NetPosition over all bindings for that Ticker in the Portfolio).
- Positions and P&L are not stored redundantly; aggregates are computed on demand.

## C# Snippet (Domain)
public class TickerStrategyBinding
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Guid TickerId { get; set; }
    public Guid StrategyId { get; set; }

    public decimal CapitalAllocation { get; set; }
    public int MaxPosition { get; set; }
    public bool IsActive { get; set; }

    public int NetPosition { get; set; }
    public decimal AvgEntryPrice { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
}
