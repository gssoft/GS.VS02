// Services/ClientConnection.cs

using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using NamedPipes.Interfaces;

namespace NamedPipes.Services;

public class ClientConnection : IClientConnection
{
    private readonly ILogger? _logger;
    private readonly StreamWriter _writer;
    private readonly string _channel;
    private bool _disposed;

    public string Channel => _channel;
    public NamedPipeServerStream Stream { get; }
    public bool IsConnected => Stream.IsConnected && !_disposed;

    public ClientConnection(NamedPipeServerStream stream, string channel, ILogger? logger = null)
    {
        Stream = stream;
        _channel = channel;
        _logger = logger;

        // ✅ Создаём ОДИН StreamWriter на всё время жизни соединения
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
        catch (IOException ex)
        {
            _logger?.LogError(ex, $"ClientConnection: Failed to write to {_channel} at line {System.Environment.StackTrace}");
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger?.LogError(ex, $"ClientConnection: Stream disposed for {_channel} at line {System.Environment.StackTrace}");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger?.LogDebug($"ClientConnection: Disposing connection to {_channel}");

        try { _writer?.Dispose(); } catch { }
        try { Stream?.Dispose(); } catch { }
    }
}
