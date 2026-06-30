namespace QuoteGeneratorWorker.Options;

public class StockQuoteOptions
{
    public int IntervalSeconds { get; set; } = 2;
    public required List<string> Symbols { get; set; }
}

