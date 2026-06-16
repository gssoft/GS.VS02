// Application/Interfaces/ICommandHandler.cs

namespace Application.Interfaces;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}
