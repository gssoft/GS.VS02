// Application/Events/UserCreatedEventHandler.cs

using System.Threading;
using System.Threading.Tasks;
using Application.Events; // Обработчик подписывается на конкретное событие.
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Events;
// Обработчик события. Реагирует на факт.
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // Логика реакции на событие: отправить письмо, обновить кэш и т.д.
        _logger.LogInformation("REACTING TO EVENT: Sending welcome email to new user: {@Username}", @event.Username);

        // Имитация отправки email или другой долгой операции.
        return Task.CompletedTask;
    }
}
