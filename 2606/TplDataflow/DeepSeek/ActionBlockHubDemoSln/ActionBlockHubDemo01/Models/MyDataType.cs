// Models/MyDataTypes.cs

namespace ActionBlockHubDemo.Models
{
    public class MyDataType
    {
        public required string Key { get; set; } // Ключ для маршрутизации (A, B, C)
        public int Id { get; set; }
        public required string Source { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"Id: {Id}, Key: {Key}, Source: {Source}, Time: {Timestamp:HH:mm:ss.fff}";
        }
    }
}
