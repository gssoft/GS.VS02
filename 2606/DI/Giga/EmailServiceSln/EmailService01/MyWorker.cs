// MyWorker.cs

using EmailService01;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// Наследуемся от BackgroundService, чтобы получить готовую инфраструктуру для фоновой задачи.
public class MyWorker : BackgroundService
{
    private const string Message = "MyWorker: ";
    private readonly ILogger<MyWorker> _logger;
    private readonly IEmailSender _emailSender;

    // Конструктор, через который внедряются зависимости.
    // ILogger и IEmailSender будут автоматически предоставлены контейнером DI.
    public MyWorker(ILogger<MyWorker> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }

    // Этот метод содержит логику, которая выполняется в фоновом режиме.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Цикл будет работать, пока приложение не получит сигнал к остановке.
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                try
                {

                    // Вызов метода из внедренной зависимости.
                    await _emailSender.SendEmailAsync(
                        "user@example.com",
                        "Привет от сервиса!",
                        "Это тестовое письмо, отправленное из фонового сервиса .NET."
                    );

                    // Ожидаем 5 секунд перед следующей итерацией.
                    // Метод принимает CancellationToken, чтобы корректно завершить ожидание при остановке.
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Логируем, если была отменена сама операция публикации.
                    _logger.LogWarning("Publishing of event was cancelled by StoppingToken.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(message: ex.Message, 
                        args: "Error occurred while publishing event .");
                }
            }

            _logger.LogInformation("Worker try to stopped.");
        }
        finally
        {
            _logger.LogInformation(
                message: Message,
                args: "Worker has been stopped.");
        }
    }
}
