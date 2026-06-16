// BusMicro/MessageRouter.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BusMicro;

internal sealed class MessageRouter : BackgroundService
{
    private readonly ChannelReader<IMessage> _reader; // Изменили тип параметра
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageRouter> _logger;

    // Изменили сигнатуру конструктора
    public MessageRouter(ChannelReader<IMessage> reader, IServiceProvider serviceProvider, ILogger<MessageRouter> logger)
    {
        _reader = reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageRouter начал обработку сообщений...");

        // Используем _reader для чтения сообщений
        await foreach (var message in _reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            await RouteMessageAsync(message, stoppingToken);
        }

        _logger.LogInformation("MessageRouter завершил обработку всех сообщений.");
    }

    private async Task RouteMessageAsync(IMessage message, CancellationToken ct)
    {
        var messageType = message.GetType();
        var handlerInterface = typeof(IHandler<>).MakeGenericType(messageType);

        // Получаем все зарегистрированные обработчики для данного типа сообщения
        var handlers = _serviceProvider.GetServices(handlerInterface);

        if (!handlers.Any())
        {
            _logger.LogWarning($"Не найдено ни одного обработчика для сообщения типа '{messageType.Name}'.");
            return;
        }

        // Можно вынести стратегию в конфигурацию
        var executionMode = ExecutionMode.Parallel;

        await ExecutionManager.ExecuteHandlersAsync(handlers, message, executionMode, _logger, ct);
    }
}
