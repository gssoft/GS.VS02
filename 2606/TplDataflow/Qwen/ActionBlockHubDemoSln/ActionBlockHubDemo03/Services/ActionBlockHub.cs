// Services/ActionBlockHub.cs

using ActionBlockHubDemo.Models;
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

        // Словарь для хранения счетчиков по каждому ключу
        private readonly ConcurrentDictionary<TKey, (long Processed, long Errors)> _counters = new();

        private readonly ILogger _logger; // Добавляем логгер
        // 1. Добавляем зависимость от DLQ
        private readonly IDeadLetterQueue<TMessage> _deadLetterQueue;


        public ActionBlockHub(IEnumerable<TKey> keys,
            Func<TKey, Func<TMessage, Task>> handlerFactory,
            ILogger logger,
            IDeadLetterQueue<TMessage> deadLetterQueue, // <-- Новое
            ExecutionDataflowBlockOptions? blockOptions = null)
        {
            _logger = logger;
            _deadLetterQueue = deadLetterQueue; // <-- Новое

            if (keys == null || !keys.Any())
                throw new ArgumentException("Список ключей не может быть пустым.", nameof(keys));

            var options = blockOptions ?? new ExecutionDataflowBlockOptions();

            foreach (var key in keys)
            {
                _counters[key] = (0, 0); // Инициализируем счетчики
                var handler = handlerFactory(key);

                var block = new ActionBlock<TMessage>(async msg =>
                {
                    try
                    {
                        await handler(msg).ConfigureAwait(false);
                        _counters.AddOrUpdate(key, (1, 0), (_, c) => (c.Processed + 1, c.Errors));
                    }
                    catch (Exception ex)
                    {
                        // Теперь ошибка красиво пишется в общий лог приложения
                        //_logger.LogError(ex, "Ошибка в обработчике для ключа '{Key}' при обработке сообщения.", key);
                        //_counters.AddOrUpdate(key, (0, 1), (_, c) => (c.Processed, c.Errors + 1));
                        _logger.LogError(ex, "Ошибка в обработчике для ключа '{Key}'. Сообщение отправлено в Dead Letter Queue.", key);
                        _counters.AddOrUpdate(key, (0, 1), (_, c) => (c.Processed, c.Errors + 1));

                        // ⚠️ УБЕДИТЕСЬ, ЧТО ЭТИ СТРОКИ ЕСТЬ В ВАШЕМ КОДЕ:
                        var deadMsg = new DeadLetterMessage<TMessage>
                        {
                            OriginalMessage = msg,
                            Key = key.ToString(),
                            ErrorMessage = ex.Message,
                            StackTrace = ex.StackTrace ?? "No stack trace"
                        };

                        // ⚠️ И САМ ВЫЗОВ ОЧЕРЕДИ:
                        _deadLetterQueue.Enqueue(deadMsg);
                    }
                }, options);

                _blocks[key] = (block, block.Completion);
            }
        }

        public long GetProcessedCount(TKey key) => _counters.TryGetValue(key, out var c) ? c.Processed : 0;
        public long GetErrorCount(TKey key) => _counters.TryGetValue(key, out var c) ? c.Errors : 0;
        
        // Post -> SendAsync 
        public async Task PublishAsync(TKey key, TMessage message)
        {
            if (_blocks.TryGetValue(key, out var blockInfo))
            {
                if (!await blockInfo.Block.SendAsync(message))
                    throw new InvalidOperationException($"Невозможно опубликовать сообщение. Блок для ключа '{key}' завершен.");
                // return Task.CompletedTask;
            }
            else
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
