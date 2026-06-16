// // Interfaces/IPublisherService.cs

namespace NamedPipes.Interfaces;

public interface IPublisherService
{
    void Publish(string ticker, string message);
    void Subscribe(string ticker, Func<string, Task> handler);
}
