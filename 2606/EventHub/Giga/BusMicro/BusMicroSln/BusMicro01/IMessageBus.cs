// BusMicro/IMessageBus.cs
using System.Threading;
using System.Threading.Tasks;

namespace BusMicro;

public interface IMessageBus
{
    ValueTask PublishAsync(IMessage message, CancellationToken ct = default);
    ValueTask SendAsync(ICommand command, CancellationToken ct = default);
}
