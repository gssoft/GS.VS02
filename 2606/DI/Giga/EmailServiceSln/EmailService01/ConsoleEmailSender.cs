// ConsoleEmailSender.cs

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

// Реализуем интерфейс IEmailSender.
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    // Внедряем логгер через конструктор.
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    // Реализация метода отправки.
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("--- Отправка email ---");
            _logger.LogInformation("Кому: {To}", to);
            _logger.LogInformation("Тема: {Subject}", subject);
            _logger.LogInformation("Тело: {Body}", body);
            _logger.LogInformation("--- Email успешно 'отправлен' в консоль. ---");
        });
    }
}
