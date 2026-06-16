// Dataflow/QuoteChannel.cs
using System.Threading.Tasks.Dataflow;
using WorkerService.Models;

namespace WorkerService.Dataflow
{
    public class QuoteChannel
    {
        // Канал от Publisher к Subscriber
        public BufferBlock<Quote> PublisherToSubscriber { get; }

        // Канал от Subscriber к Publisher (обратная связь)
        public BufferBlock<Quote> SubscriberToPublisher { get; }

        // Broadcast для мониторинга (многие подписчики)
        public BroadcastBlock<Quote> MonitoringBroadcast { get; }

        public QuoteChannel()
        {
            // Основной канал публикаций
            PublisherToSubscriber = new BufferBlock<Quote>(new DataflowBlockOptions
            {
                BoundedCapacity = 100, // Ограничиваем очередь
                NameFormat = "PublisherToSubscriber"
            });

            // Обратный канал
            SubscriberToPublisher = new BufferBlock<Quote>(new DataflowBlockOptions
            {
                BoundedCapacity = 50,
                NameFormat = "SubscriberToPublisher"
            });

            // Broadcast для мониторинга (рассылка всем)
            MonitoringBroadcast = new BroadcastBlock<Quote>(null, new DataflowBlockOptions
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                NameFormat = "MonitoringBroadcast"
            });
        }
    }
}
