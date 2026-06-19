using ActionBlockHubDemo.Models;
using System.Collections.Generic;

namespace ActionBlockHubDemo.Services
{
    public interface IDeadLetterQueue<TMessage>
    {
        void Enqueue(DeadLetterMessage<TMessage> message);
        IReadOnlyList<DeadLetterMessage<TMessage>> GetAll();
        void Clear();
    }
}
