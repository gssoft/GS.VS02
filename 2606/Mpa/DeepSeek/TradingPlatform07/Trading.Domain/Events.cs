// Events.cs
namespace Trading.Domain;

public record Quote(string Ticker, decimal Bid, decimal Ask, DateTime Timestamp);
// public record NewQuotes(IReadOnlyList<Quote> Quotes);
public record NewQuotes(List<Quote> Quotes);

public record OrderRequested(string Ticker, decimal Quantity, decimal Price, string Side); // Buy/Sell
public record OrderCreated(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
public record OrderFilled(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side);

public record OrderNotFilled(Guid OrderId, string Ticker, string Reason);
// public record OrderNotFilled(Guid OrderId, string Reason);

public record Trade(Guid TradeId, Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
public record PositionUpdated(string Ticker, decimal Quantity, decimal AveragePrice);
public record PortfolioSnapshot(IReadOnlyDictionary<string, decimal> Positions);


