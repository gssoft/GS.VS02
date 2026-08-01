// File: Contracts/QuoteDto.cs
using System.Text.Json.Serialization;

namespace Quotes.Shared.Contracts;

public sealed record QuoteDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("last")] decimal Last,
    [property: JsonPropertyName("volume")] int Volume,
    [property: JsonPropertyName("timestampUtc")] DateTimeOffset TimestampUtc);
