// Application/Events/UserCreatedEvent.cs

using Application.Interfaces;

namespace Application.Events;
// DTO для события. Просто данные о факте.
public class UserCreatedEvent : IEvent
{
    public string Username { get; set; } = string.Empty; // Данные о событии.
}

