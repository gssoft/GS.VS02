// Services/ActionBlockHub.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActionBlockHubDemo.Services
{
    // 26.06.19

    // 1. Добавляем реализацию интерфейса
    public class ActionBlockHub<TKey, TMessage> : IActionBlockHub<TKey, TMessage> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, (ActionBlock<TMessage> Block, Task Completion)> _blocks = new();
        private readonly ConcurrentDictionary<TKey, Func<TMessage, Task>> _handlers = new();

        public ActionBlockHub(IEnumerable<TKey> keys, Func<TKey, Func<TMessage, Task>> handlerFactory, ExecutionDataflowBlockOptions? blockOptions = null)
        {
            if (keys == null || !keys.Any())
                throw new ArgumentException("Список ключей не может быть пустым.", nameof(keys));

            var options = blockOptions ?? new ExecutionDataflowBlockOptions();

            foreach (var key in keys)
            {
                var handler = handlerFactory(key);
                _handlers[key] = handler;

                // ВАЖНО: Оборачиваем вызов в try-catch, чтобы при ошибке блок не перешел в состояние Faulted
                // и не остановил всю обработку.
                var block = new ActionBlock<TMessage>(async msg =>
                {
                    try
                    {
                        await handler(msg).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Здесь можно залогировать ошибку, если передать ILogger в ActionBlockHub
                        Console.WriteLine($"Ошибка в обработчике для ключа {key}: {ex.Message}");
                    }
                }, options);

                _blocks[key] = (block, block.Completion);
            }
        }

        public Task PublishAsync(TKey key, TMessage message)
        {
            if (_blocks.TryGetValue(key, out var blockInfo))
            {
                if (!blockInfo.Block.Post(message))
                    throw new InvalidOperationException($"Невозможно опубликовать сообщение. Блок для ключа '{key}' завершен.");
                return Task.CompletedTask;
            }
            throw new KeyNotFoundException($"Канал с ключом '{key}' не найден.");
        }

        // 2. Добавляем метод для получения целевого блока (нужен для LinkTo)
        public ITargetBlock<TMessage> GetTargetBlock(TKey key)
        {
            if (_blocks.TryGetValue(key, out var blockInfo))
                return blockInfo.Block;

            throw new KeyNotFoundException($"Блок для ключа '{key}' не найден.");
        }

        public void Complete()
        {
            foreach (var (_, blockInfo) in _blocks)
                blockInfo.Block.Complete();
        }

        public Task Completion => Task.WhenAll(_blocks.Values.Select(bi => bi.Completion));
    }
}

//namespace ActionBlockHubDemo.Services
//{
//    // Добавлено ограничение where TKey : notnull
//    // Это убирает предупреждения CS8714 и гарантирует, что ключи не будут null.
//    public class ActionBlockHub<TKey, TMessage> where TKey : notnull
//    {
//        private readonly ConcurrentDictionary<TKey, (ActionBlock<TMessage> Block, Task Completion)> _blocks = new();
//        private readonly ConcurrentDictionary<TKey, Func<TMessage, Task>> _handlers = new();

//        public ActionBlockHub(IEnumerable<TKey> keys, Func<TKey, Func<TMessage, Task>> handlerFactory, ExecutionDataflowBlockOptions? blockOptions = null)
//        {
//            // ... (проверка keys)

//            if (keys == null || !keys.Any())
//                throw new ArgumentException("Список ключей не может быть пустым.", nameof(keys));

//            var options = blockOptions ?? new ExecutionDataflowBlockOptions(); // Если не передали, используем дефолтные

//            foreach (var key in keys)
//            {
//                var handler = handlerFactory(key);
//                _handlers[key] = handler;

//                // Используем переданные опции при создании блока
//                var block = new ActionBlock<TMessage>(async msg => await handler(msg).ConfigureAwait(false), options);

//                _blocks[key] = (block, block.Completion);
//            }
//        }

//        public Task PublishAsync(TKey key, TMessage message)
//        {
//            if (_blocks.TryGetValue(key, out var blockInfo))
//            {
//                if (!blockInfo.Block.Post(message))
//                {
//                    throw new InvalidOperationException($"Невозможно опубликовать сообщение. Блок для ключа '{key}' завершен.");
//                }
//                return Task.CompletedTask;
//            }
//            throw new KeyNotFoundException($"Канал с ключом '{key}' не найден.");
//        }

//        public void Complete()
//        {
//            foreach (var (_, blockInfo) in _blocks)
//            {
//                blockInfo.Block.Complete();
//            }
//        }

//        public Task Completion => Task.WhenAll(_blocks.Values.Select(bi => bi.Completion));

//        // 26.06.19
//        public ITargetBlock<TMessage> GetTargetBlock(TKey key)
//        {
//            if (_blocks.TryGetValue(key, out var blockInfo))
//            {
//                return blockInfo.Block;
//            }
//            throw new KeyNotFoundException($"Блок для ключа '{key}' не найден.");
//        }
//    }
//}
