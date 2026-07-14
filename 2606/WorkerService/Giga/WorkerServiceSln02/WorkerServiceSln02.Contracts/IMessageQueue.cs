namespace WorkerServiceSln02.Contracts;

public interface IMessageQueue
{
    void Enqueue(string msg);
    bool TryDequeue(out string? msg);
}
