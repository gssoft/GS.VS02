namespace ActionBlockHubDemo.Options
{
    public class HubOptions
    {
        public ActionBlockSettings ActionBlock { get; set; } = new();
        public GeneratorSettings Generator { get; set; } = new();
    }

    public class ActionBlockSettings
    {
        // ✅ ИСПРАВЛЕНИЕ: Инициализируем пустым списком. 
        // Теперь .NET просто заполнит его данными из JSON.
        public List<string> Keys { get; set; } = new();

        public int BoundedCapacity { get; set; } = 100;
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }

    public class GeneratorSettings
    {
        public int IntervalMs { get; set; } = 1000;
    }
}

//namespace ActionBlockHubDemo.Options
//{
//    public class HubOptions
//    {
//        // Секция для ActionBlockHub
//        public ActionBlockSettings ActionBlock { get; set; } = new();

//        // Секция для генератора данных
//        public GeneratorSettings Generator { get; set; } = new();
//    }

//    public class ActionBlockSettings
//    {
//        public List<string> Keys { get; set; } = new() { "A", "B", "C" };
//        public int BoundedCapacity { get; set; } = 100;
//        public int MaxDegreeOfParallelism { get; set; } = 1;
//    }

//    public class GeneratorSettings
//    {
//        public int IntervalMs { get; set; } = 1000; // Интервал генерации в миллисекундах
//    }
//}
