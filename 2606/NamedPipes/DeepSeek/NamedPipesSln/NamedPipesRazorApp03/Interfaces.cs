// Interfaces.cs
public interface IPublisherService
{
    void Publish(string ticker, string message);
    void Subscribe(string v, Func<string, bool> firstChannelHandler);
}

public interface ISubscriberService
{
    void Subscribe(string ticker, Func<string, bool> handler);
}

public class EventHub : Dictionary<string, List<Func<string, bool>>>, IPublisherService, ISubscriberService
{
    public void AddHandler(string ticker, Func<string, bool> handler)
    {
        if (!ContainsKey(ticker))
            this[ticker] = new List<Func<string, bool>>();
        this[ticker].Add(handler);
    }

    public void InvokeHandlers(string ticker, string message)
    {
        if (TryGetValue(ticker, out var handlers))
        {
            foreach (var handler in handlers)
                handler(message);
        }
    }

    public void Publish(string ticker, string message)
    {
        InvokeHandlers(ticker, message);
    }

    public void Subscribe(string ticker, Func<string, bool> handler)
    {
        AddHandler(ticker, handler);
    }
}