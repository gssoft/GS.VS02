// Services/ClientConnection.cs

using System.IO.Pipes;
using QuotesServer.Interfaces;

namespace QuotesServer.Services;

public class ClientConnection : IClientConnection
{
    private readonly StreamWriter _writer;
    private readonly string _channel;
    private bool _disposed;

    public string Channel => _channel;
    public NamedPipeServerStream Stream { get; }
    public bool IsConnected => Stream.IsConnected && !_disposed;

    public ClientConnection(NamedPipeServerStream stream, string channel)
    {
        Stream = stream;
        _channel = channel;

        _writer = new StreamWriter(stream)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
    }

    public async Task WriteAsync(string data)
    {
        if (_disposed || !Stream.IsConnected)
            throw new IOException($"Client disconnected from {_channel}");

        try
        {
            await _writer.WriteLineAsync(data);
        }
        catch
        {
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _writer?.Dispose(); } catch { }
        try { Stream?.Dispose(); } catch { }
    }
}
