// IEmailSender.cs

public interface IEmailSender
{
    // Определяем асинхронный метод для отправки email.
    Task SendEmailAsync(string to, string subject, string body);
}
