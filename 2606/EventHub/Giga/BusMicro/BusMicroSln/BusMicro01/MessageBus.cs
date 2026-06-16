// BusMicro/MessageBus.cs
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace BusMicro;

internal sealed class MessageBus : IMessageBus
{
    private readonly ChannelWriter<IMessage> _writer; // Изменили тип поля и параметра
    private readonly ILogger<MessageBus> _logger;

    // Изменили сигнатуру конструктора
    public MessageBus(ChannelWriter<IMessage> writer, ILogger<MessageBus> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    public async ValueTask PublishAsync(IMessage message, CancellationToken ct = default)
    {
        await _writer.WriteAsync(message, ct); // Используем _writer для записи
        _logger.LogDebug($"Событие '{message.GetType().Name}' опубликовано.");
    }

    public async ValueTask SendAsync(ICommand command, CancellationToken ct = default)
    {
        await _writer.WriteAsync(command, ct); // Используем _writer для записи
        _logger.LogInformation($"Команда '{command.GetType().Name}' отправлена на обработку.");
    }
}

