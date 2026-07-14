using System.Collections.Concurrent;

namespace WorkerServiceSln02.Contracts;

// Обратите внимание: наследуемся от ConcurrentQueue<string>
public class InMemoryQueue : ConcurrentQueue<string>, IMessageQueue
{
    public void Enqueue(string msg) => base.Enqueue(msg);
    public bool TryDequeue(out string? msg) => base.TryDequeue(out msg);
}
