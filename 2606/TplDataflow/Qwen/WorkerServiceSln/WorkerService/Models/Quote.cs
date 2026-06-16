// Models/Quote.cs
namespace WorkerService.Models
{
    public class Quote
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] {Symbol}: {Price:C} (от {Source})";
    }
}
