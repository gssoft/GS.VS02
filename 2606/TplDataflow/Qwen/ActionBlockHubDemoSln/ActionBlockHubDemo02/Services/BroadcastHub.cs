// Services/BroadcastHub.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActionBlockHubDemo.Services
{
    // 1. Добавляем реализацию интерфейса
    public class BroadcastHub<TKey, TMessage> : IBroadcastHub<TKey, TMessage> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, BroadcastBlock<TMessage>> _broadcastBlocks = new();

        public BroadcastHub(IEnumerable<TKey> keys)
        {
            if (keys == null || !keys.Any())
                throw new ArgumentException("Список ключей не может быть пустым.", nameof(keys));

            foreach (var key in keys)
                _broadcastBlocks[key] = new BroadcastBlock<TMessage>(null);
        }

        public Task PublishAsync(TKey key, TMessage message)
        {
            if (_broadcastBlocks.TryGetValue(key, out var block))
                return block.SendAsync(message);

            throw new KeyNotFoundException($"Канал с ключом '{key}' не найден.");
        }

        public IDisposable LinkTo(TKey key, ITargetBlock<TMessage> targetBlock, DataflowLinkOptions? linkOptions = null)
        {
            if (_broadcastBlocks.TryGetValue(key, out var sourceBlock))
            {
                // Используем переданные опции (например, PropagateCompletion) или дефолтные
                return sourceBlock.LinkTo(targetBlock, linkOptions ?? new DataflowLinkOptions());
            }
            throw new KeyNotFoundException($"Канал с ключом '{key}' не найден.");
        }

        // 2. Добавляем методы для корректного завершения (Graceful Shutdown)
        public void Complete()
        {
            foreach (var block in _broadcastBlocks.Values)
                block.Complete();
        }

        public Task Completion => Task.WhenAll(_broadcastBlocks.Values.Select(b => b.Completion));
    }
}
