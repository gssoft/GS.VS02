using BusLibrary;

[MessageKey("quote:generated")]
public record QuoteGeneratedEvent(
    string Symbol,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    DateTime Timestamp
) : MessageBase("quote:generated");
