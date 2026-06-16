// Application/Interfaces/ICommand.cs

namespace Application.Interfaces;

// Наследуем от IMessage для полиморфизма, если понадобится
public interface ICommand : IMessage { }
