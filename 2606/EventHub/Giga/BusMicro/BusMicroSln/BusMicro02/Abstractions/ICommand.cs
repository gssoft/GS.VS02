// BusMicro/Abstractions/ICommand.cs

namespace BusMicro;

/// <summary>
/// Интерфейс для команды. Команда представляет собой намерение выполнить действие.
/// Наследуется от IMessage для унификации обработки.
/// </summary>
public interface ICommand : IMessage { }

public class Command : ICommand { }