// Messages/MyEvent.cs

namespace EventHubWorkerService.Messages;

// Если у вас есть базовый интерфейс сообщений в EventBus.Abstractions, наследуйте его.
// Если нет — можно оставить как есть.
public record MyEvent(Guid Id, string Payload, DateTime Timestamp);
