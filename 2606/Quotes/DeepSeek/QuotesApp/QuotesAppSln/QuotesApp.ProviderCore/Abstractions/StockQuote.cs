using System.Text.Json.Serialization;

namespace QuotesApp.Shared.Models;

public record StockQuote(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("bid")] double Bid,
    [property: JsonPropertyName("ask")] double Ask,
    [property: JsonPropertyName("last")] double Last,
    [property: JsonPropertyName("volume")] int Volume,
    [property: JsonPropertyName("portfolio")] string Portfolio,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
