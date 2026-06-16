// BusMicro/ExecutionManager.cs
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BusMicro;

internal static class ExecutionManager
{
    public static async Task ExecuteHandlersAsync(
        IEnumerable<object> handlers,
        object message,
        ExecutionMode executionMode,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (!handlers.Any())
            return;

        var handlerType = typeof(IHandler<>).MakeGenericType(message.GetType());
        var methodInfo = handlerType.GetMethod("HandleAsync");

        switch (executionMode)
        {
            case ExecutionMode.Parallel:
                var tasks = handlers.Select(handler =>
                    (Task)methodInfo.Invoke(handler, new[] { message, ct })!);
                await Task.WhenAll(tasks);
                break;

            case ExecutionMode.Sequential:
                foreach (var handler in handlers)
                {
                    try
                    {
                        await (Task)methodInfo.Invoke(handler, new[] { message, ct })!;
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку конкретного обработчика, но продолжаем выполнение остальных.
                        logger.LogError(ex, $"Ошибка при обработке сообщения '{message.GetType().Name}' обработчиком '{handler.GetType().Name}'.");
                    }
                }
                break;
        }
    }
}
