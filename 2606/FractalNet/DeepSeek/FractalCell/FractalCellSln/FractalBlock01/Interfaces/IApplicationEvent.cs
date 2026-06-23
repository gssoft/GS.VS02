// Core/Interfaces/IApplicationEvent.cs

//public interface IApplicationEvent
//{
//    string EventId { get; }
//    DateTime Timestamp { get; }
//}

// Базовый контракт для всех событий приложения
public interface IApplicationEvent
{
    string EventId { get; }           // Уникальный идентификатор события
    DateTime Timestamp { get; }      // Время возникновения
    string SourceBlockId { get; }     // ID блока-источника
    string TargetBlockId { get; }     // ID блока-получателя (для явной маршрутизации)
    string EventType { get; }         // Тип события (например, "DataGenerated")
}
