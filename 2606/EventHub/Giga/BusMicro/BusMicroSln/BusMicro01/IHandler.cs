// BusMicro/IHandler.cs
using System.Threading;
using System.Threading.Tasks;

namespace BusMicro;

/// <summary>
/// Обработчик сообщений определенного типа.
/// </summary>
/// <typeparam name="TMessage">Тип сообщения, которое обрабатывает данный класс.</typeparam>
public interface IHandler<in TMessage> where TMessage : IMessage
{
    /// <summary>
    /// Асинхронно обрабатывает сообщение.
    /// </summary>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
