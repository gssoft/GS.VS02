namespace NamedPipes.Interfaces;

public interface ISubscriberService
{
    void Subscribe(string ticker, Func<string, Task> handler);
}
