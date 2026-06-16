// Application/Interfaces/ICommandBus.cs

using Application.Interfaces;

public interface ICommandBus
{
    ValueTask SendAsync(ICommand command, CancellationToken ct = default);
}
