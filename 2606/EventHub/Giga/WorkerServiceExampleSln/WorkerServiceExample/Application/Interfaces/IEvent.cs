// Application/Interfaces/IEvent.cs

namespace Application.Interfaces;

// Наследуем от IMessage для полиморфизма, если понадобится
public interface IEvent : IMessage { }