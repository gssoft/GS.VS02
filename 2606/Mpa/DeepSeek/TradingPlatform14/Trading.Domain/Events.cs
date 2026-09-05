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

