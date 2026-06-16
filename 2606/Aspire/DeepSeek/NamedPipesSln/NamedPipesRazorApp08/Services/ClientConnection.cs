// Services/ClientConnection.cs

using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using NamedPipes.Interfaces;

namespace NamedPipes.Services;

public class ClientConnection : IClientConnection, IDisposable
{
    private readonly ILogger? _logger;
    private readonly StreamWriter _writer;
    private bool _disposed;

    public string Channel { get; }
    public NamedPipeServerStream Stream { get; }
    public bool IsConnected => Stream.IsConnected && !_disposed;

    public ClientConnection(NamedPipeServerStream stream, string channel, ILogger? logger = null)
    {
        Stream = stream;
        Channel = channel;
        _logger = logger;

        // ✅ Создаём ОДИН StreamWriter на всё время жизни соединения
        _writer = new StreamWriter(stream)
        {
            AutoFlush = true
        };
    }

    public async Task WriteAsync(string data)
    {
        if (_disposed || !Stream.IsConnected)
            throw new IOException("Client disconnected");

        try
        {
            await _writer.WriteLineAsync(data);
            // AutoFlush = true, поэтому отдельный Flush не нужен
        }
        catch (IOException ex)
        {
            _logger?.LogDebug(ex, $"ClientConnection: Failed to write to {Channel}");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        try { _writer.Dispose(); } catch { }
        try { Stream.Dispose(); } catch { }
    }
}

//using System.IO.Pipes;
//using Microsoft.Extensions.Logging;
//using NamedPipes.Interfaces;

//namespace NamedPipes.Services;

//public class ClientConnection : IClientConnection
//{
//    private readonly ILogger? _logger;

//    public string Channel { get; }
//    public NamedPipeServerStream Stream { get; }
//    public bool IsConnected => Stream.IsConnected;

//    public ClientConnection(NamedPipeServerStream stream, string channel, ILogger? logger = null)
//    {
//        Stream = stream;
//        Channel = channel;
//        _logger = logger;
//    }

//    public async Task WriteAsync(string data)
//    {
//        try
//        {
//            if (!Stream.IsConnected)
//                return;

//            using var writer = new StreamWriter(Stream);
//            writer.AutoFlush = true;
//            await writer.WriteLineAsync(data);
//        }
//        catch (IOException ex)
//        {
//            _logger?.LogDebug(ex, $"ClientConnection: Failed to write to {Channel} (client disconnected)");
//            throw;
//        }
//        catch (ObjectDisposedException)
//        {
//            _logger?.LogDebug($"ClientConnection: Stream disposed for {Channel}");
//            throw;
//        }
//    }
//}