using ActionBlockHubDemo.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ActionBlockHubDemo.Services
{
    public class InMemoryDeadLetterQueue<TMessage> : IDeadLetterQueue<TMessage>
    {
        // ConcurrentQueue потокобезопасна, что критично, так как писать в нее будут разные потоки ActionBlock
        private readonly ConcurrentQueue<DeadLetterMessage<TMessage>> _queue = new();
        private readonly ILogger _logger;

        // Добавляем логгер, чтобы отследить создание экземпляров
        public InMemoryDeadLetterQueue(ILogger<InMemoryDeadLetterQueue<TMessage>> logger)
        {
            _logger = logger;
            // Этот лог покажет, сколько раз создается объект. Для Singleton он должен быть ОДИН.
            _logger.LogWarning("🔧 Создан новый экземпляр InMemoryDeadLetterQueue (HashCode: {HashCode})", this.GetHashCode());
        }

        //public void Enqueue(DeadLetterMessage<TMessage> message)
        //{
        //    _queue.Enqueue(message);
        //}

        public void Enqueue(DeadLetterMessage<TMessage> message)
        {
            // Логируем факт добавления и размер очереди ДО и ПОСЛЕ
            _logger.LogInformation("📥 DLQ Enqueue: HashCode={HashCode}. Размер до: {CountBefore}", this.GetHashCode(), _queue.Count);
            _queue.Enqueue(message);
            _logger.LogInformation("📥 DLQ Enqueue: Размер после: {CountAfter}", _queue.Count);
        }

        //public IReadOnlyList<DeadLetterMessage<TMessage>> GetAll()
        //{
        //    return _queue.ToList().AsReadOnly();
        //}

        public IReadOnlyList<DeadLetterMessage<TMessage>> GetAll()
        {
            // Логируем факт чтения и размер очереди
            _logger.LogInformation("📤 DLQ GetAll: HashCode={HashCode}. Текущий размер: {Count}", this.GetHashCode(), _queue.Count);
            return _queue.ToList().AsReadOnly();
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }
}
