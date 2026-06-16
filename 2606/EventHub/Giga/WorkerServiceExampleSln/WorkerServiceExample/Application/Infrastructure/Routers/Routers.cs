// Application/Infrastructure/Routers/Routers.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Routers;
// Роутер для команд. Использует TryRead для последовательной обработки.
public class CommandRouter : BackgroundService
{
    private readonly ChannelReader<ICommand> _reader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandRouter> _logger;

    public CommandRouter(ChannelReader<ICommand> reader, IServiceProvider serviceProvider, ILogger<CommandRouter> logger)
    {
        _reader = reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommandRouter is starting.");

        while (await _reader.WaitToReadAsync(stoppingToken))
        {
            while (_reader.TryRead(out var command))
            {
                if (stoppingToken.IsCancellationRequested) break;
                await RouteCommandAsync(command, stoppingToken);
            }
        }
        _logger.LogInformation("CommandRouter is stopping.");
    }

    private async Task RouteCommandAsync(ICommand command, CancellationToken ct)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler registered for command: {commandType.Name}");

        var method = handlerType.GetMethod("HandleAsync");
        await (Task)method.Invoke(handler, new object[] { command, ct })!;
    }
}
// Роутер для событий. Использует ReadAllAsync для параллельной обработки.
public class EventRouter : BackgroundService
{
    private readonly ChannelReader<IEvent> _reader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventRouter> _logger;

    public EventRouter(ChannelReader<IEvent> reader, IServiceProvider serviceProvider, ILogger<EventRouter> logger)
    {
        _reader = reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventRouter is starting.");
        await foreach (var @event in _reader.ReadAllAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;
            await RouteEventAsync(@event, stoppingToken);
        }
        _logger.LogInformation("EventRouter is stopping.");
    }

    private async Task RouteEventAsync(IEvent @event, CancellationToken ct)
    {
        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        if (!handlers.Any())
            _logger.LogWarning("No handlers found for event: {EventName}", eventType.Name);

        var tasks = handlers.Select(handler =>
            (Task)handler.GetType().GetMethod("HandleAsync")!.Invoke(handler, new object[] { @event, ct })!
        );

        await Task.WhenAll(tasks); // Параллельное выполнение всех обработчиков события.
    }
}
