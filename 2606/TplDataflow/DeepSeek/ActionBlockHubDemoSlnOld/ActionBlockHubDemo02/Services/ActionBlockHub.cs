// Services/ActionBlockHub.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActionBlockHubDemo.Services
{
    // Добавлено ограничение where TKey : notnull
    // Это убирает предупреждения CS8714 и гарантирует, что ключи не будут null.
    public class ActionBlockHub<TKey, TMessage> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, (ActionBlock<TMessage> Block, Task Completion)> _blocks = new();
        private readonly ConcurrentDictionary<TKey, Func<TMessage, Task>> _handlers = new();

        public ActionBlockHub(IEnumerable<TKey> keys, Func<TKey, Func<TMessage, Task>> handlerFactory, ExecutionDataflowBlockOptions? blockOptions = null)
        {
            // ... (проверка keys)

            if (keys == null || !keys.Any())
                throw new ArgumentException("Список ключей не может быть пустым.", nameof(keys));

            var options = blockOptions ?? new ExecutionDataflowBlockOptions(); // Если не передали, используем дефолтные

            foreach (var key in keys)
            {
                var handler = handlerFactory(key);
                _handlers[key] = handler;

                // Используем переданные опции при создании блока
                var block = new ActionBlock<TMessage>(async msg => await handler(msg).ConfigureAwait(false), options);

                _blocks[key] = (block, block.Completion);
            }
        }

        public Task PublishAsync(TKey key, TMessage message)
        {
            if (_blocks.TryGetValue(key, out var blockInfo))
            {
                if (!blockInfo.Block.Post(message))
                {
                    throw new InvalidOperationException($"Невозможно опубликовать сообщение. Блок для ключа '{key}' завершен.");
                }
                return Task.CompletedTask;
            }
            throw new KeyNotFoundException($"Канал с ключом '{key}' не найден.");
        }

        public void Complete()
        {
            foreach (var (_, blockInfo) in _blocks)
            {
                blockInfo.Block.Complete();
            }
        }

        public Task Completion => Task.WhenAll(_blocks.Values.Select(bi => bi.Completion));
    }
}
