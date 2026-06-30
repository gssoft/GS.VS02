// Options/StockQuoteOptions.cs

namespace QuoteGeneratorWorker.Options;

public class StockQuoteOptions
{
    /// <summary>
    /// Интервал генерации котировок в секундах.
    /// </summary>
    public int IntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Список тикеров акций.
    /// </summary>
    public List<string> Symbols { get; set; } = new() { "GAZP", "SBER", "LKOH" };
}

