// Interfaces/IClientConnection.cs

using System.IO.Pipes;

namespace QuotesServer.Interfaces;

public interface IClientConnection : IDisposable
{
    string Channel { get; }
    NamedPipeServerStream Stream { get; }
    bool IsConnected { get; }
    Task WriteAsync(string data);
}