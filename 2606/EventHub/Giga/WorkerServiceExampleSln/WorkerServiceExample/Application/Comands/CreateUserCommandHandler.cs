// Application/Comands/CreateUserCommandHandler.cs

using System.Threading;
using System.Threading.Tasks;
using Application.Commands;
using Application.Interfaces;
using Application.Events; // Важно: хендлер команды публикует событие!
using Microsoft.Extensions.Logging;

namespace Application.Commands;
// Обработчик команды. Здесь сосредоточена бизнес-логика.
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IEventBus _eventBus; // Инжектим шину событий для публикации результата.
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IEventBus eventBus, ILogger<CreateUserCommandHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        // 1. Бизнес-логика: создание пользователя (здесь просто логируем)
        _logger.LogInformation("Creating user with name: {Username}", command.Username);

        // Имитация асинхронной операции (например, запись в БД)
        await Task.Delay(100, ct);

        // 2. Публикация события о том, что пользователь создан.
        var userCreatedEvent = new UserCreatedEvent { Username = command.Username };

        // Fire and Forget. Мы не ждем завершения обработки события.
        await _eventBus.PublishAsync(userCreatedEvent, ct);

        _logger.LogInformation("User created and event published.");
    }
}
