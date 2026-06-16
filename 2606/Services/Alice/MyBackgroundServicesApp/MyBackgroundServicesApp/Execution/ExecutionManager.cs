// Execution/ExecutionManager.cs

using System.Reflection;
using System.Threading;
using System.Collections.Generic;

public class ExecutionManager
{
    public static async Task ExecuteHandlersAsync<TMessage>(
        IEnumerable<object> handlers,
        TMessage message,
        ExecutionMode mode,
        CancellationToken ct) where TMessage : IMessage
    {
        if (mode == ExecutionMode.Parallel)
        {
            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                if (handler == null) continue; // Пропускаем null-элементы

                var handleMethod = handler.GetType()
                    .GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"Method HandleAsync not found on {handler.GetType()}");

                var task = (Task)handleMethod.Invoke(handler, new object[] { message, ct })!;
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue; // Пропускаем null-элементы

                var handleMethod = handler.GetType()
            .GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"Method HandleAsync not found on {handler.GetType()}");
                await (Task)handleMethod.Invoke(handler, new object[] { message, ct })!;
            }
        }
    }
}


//public class ExecutionManager
//{
//    public static async Task ExecuteHandlersAsync<TMessage>(
//        IEnumerable<object> handlers,
//        TMessage message,
//        ExecutionMode mode,
//        CancellationToken ct) where TMessage : IMessage
//    {
//        if (mode == ExecutionMode.Parallel)
//        {
//            var tasks = new List<Task>();
//            foreach (var handler in handlers)
//            {
//                var handleMethod = handler.GetType()
//                    .GetMethod("HandleAsync")
//            ?? throw new InvalidOperationException($"Method HandleAsync not found on {handler.GetType()}");

//                var task = (Task)handleMethod.Invoke(handler, new object[] { message, ct })!;
//                tasks.Add(task);
//            }
//            await Task.WhenAll(tasks);
//        }
//        else
//        {
//            foreach (var handler in handlers)
//            {
//                var handleMethod = handler.GetType()
//                    .GetMethod("HandleAsync")
//            ?? throw new InvalidOperationException($"Method HandleAsync not found on {handler.GetType()}");
//                await (Task)handleMethod.Invoke(handler, new object[] { message, ct })!;
//            }
//        }
//    }
//}


//class ExecutionManager
//{
//    public static async Task ExecuteHandlersAsync<TMessage>(
//        IEnumerable<IHandler<TMessage>> handlers,
//        TMessage message,
//        ExecutionMode mode,
//        CancellationToken ct) where TMessage : IMessage
//    {
//        if (mode == ExecutionMode.Parallel)
//        {
//            var tasks = handlers.Select(h => h.HandleAsync(message, ct));
//            await Task.WhenAll(tasks);
//        }
//        else
//        {
//            foreach (var handler in handlers)
//            {
//                await handler.HandleAsync(message, ct);
//            }
//        }
//    }
//}

