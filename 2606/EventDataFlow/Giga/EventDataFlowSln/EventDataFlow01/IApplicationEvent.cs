// IApplicationEvent.cs
public interface IApplicationEvent { }

// Пример события
public record UserRegisteredEvent(string Username) : IApplicationEvent;
