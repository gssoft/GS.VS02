// Events.cs

namespace Trading.Domain;

public class Quote
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public DateTime Timestamp { get; set; }
}

public class NewQuotes
{
    public List<Quote> Quotes { get; set; } = new();
}

public class OrderRequested
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Side { get; set; } = string.Empty;
}

public class OrderCreated
{
    public Guid OrderId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Side { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class OrderFilled
{
    public Guid OrderId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Side { get; set; } = string.Empty;
}

public class OrderNotFilled
{
    public Guid OrderId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class Trade
{
    public Guid TradeId { get; set; }
    public Guid OrderId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Side { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class PositionUpdated
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
}

public class PortfolioSnapshot
{
    public Dictionary<string, decimal> Positions { get; set; } = new();
}

//// Events.cs
//namespace Trading.Domain;

//public record Quote(string Ticker, decimal Bid, decimal Ask, DateTime Timestamp);

//public class NewQuotes
//{
//    public List<Quote> Quotes { get; set; } = new();
//}

//public record OrderRequested(string Ticker, decimal Quantity, decimal Price, string Side);
//public record OrderCreated(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
//public record OrderFilled(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side);
//public record OrderNotFilled(Guid OrderId, string Ticker, string Reason);

//public record Trade(Guid TradeId, Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
//public record PositionUpdated(string Ticker, decimal Quantity, decimal AveragePrice);
//public record PortfolioSnapshot(Dictionary<string, decimal> Positions);


//namespace Trading.Domain;

//public record Quote(string Ticker, decimal Bid, decimal Ask, DateTime Timestamp);
//// public record NewQuotes(IReadOnlyList<Quote> Quotes);
//public record NewQuotes(List<Quote> Quotes);

//public record OrderRequested(string Ticker, decimal Quantity, decimal Price, string Side); // Buy/Sell
//public record OrderCreated(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
//public record OrderFilled(Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side);

//public record OrderNotFilled(Guid OrderId, string Ticker, string Reason);
//// public record OrderNotFilled(Guid OrderId, string Reason);

//public record Trade(Guid TradeId, Guid OrderId, string Ticker, decimal Quantity, decimal Price, string Side, DateTime Timestamp);
//public record PositionUpdated(string Ticker, decimal Quantity, decimal AveragePrice);
//public record PortfolioSnapshot(IReadOnlyDictionary<string, decimal> Positions);


