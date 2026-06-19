// Interfaces/Interfaces

using System.Threading.Tasks.Dataflow;

public interface IBroadcastHub<TKey, TMessage> where TKey : notnull
{
    Task PublishAsync(TKey key, TMessage message);
    IDisposable LinkTo(TKey key, ITargetBlock<TMessage> targetBlock, DataflowLinkOptions? linkOptions = null);
    void Complete();
    Task Completion { get; }
}

public interface IActionBlockHub<TKey, TMessage> where TKey : notnull
{
    Task PublishAsync(TKey key, TMessage message);
    ITargetBlock<TMessage> GetTargetBlock(TKey key); // Метод для получения блока, чтобы на него можно было подписаться
    void Complete();
    Task Completion { get; }
}
