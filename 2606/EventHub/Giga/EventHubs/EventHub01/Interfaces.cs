using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub01
{
    // Интерфейсы
    public interface IPublisherService
    {
        void Publish(string ticker, string message);
    }

    public interface ISubscriberService
    {
        void Subscribe(string ticker, Action<string> handler);
    }
}
