using System;
using System.Collections.Generic;
using System.Text;

using global::Worker.Core.Abstractions;

using System.Threading;
using System.Threading.Tasks;
// using Worker.Core.Abstractions;

namespace WorkerApp.Behaviors;
public sealed class DatabasePollingBehavior : IBehaviorStrategy
{
    // Здесь могут быть DbContext, HttpClientFactory и т.д.

    public async Task ExecuteAsync(CancellationToken token)
    {
        Console.WriteLine($"[{DateTimeOffset.Now}] Polling database for new orders...");

        // Имитация долгой работы БД
        await Task.Delay(TimeSpan.FromSeconds(2), token);

        // Если выбросится Exception — GenericJobHost поймает его и залогирует,
        // но сам сервис продолжит работу со следующей попытки бэкоффа.

        Console.WriteLine($"[{DateTimeOffset.Now}] Batch processed successfully.");
    }

    public void Dispose()
    {
        // Освобождение ресурсов контекста
    }
}
