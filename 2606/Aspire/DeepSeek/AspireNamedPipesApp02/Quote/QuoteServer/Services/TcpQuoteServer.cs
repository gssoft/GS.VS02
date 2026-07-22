// Services/TcpQuoteServer.cs
using System.Net;
using System.Net.Sockets;
using QuoteServer.Helpers;
using QuoteServer.Models;

namespace QuoteServer.Services;

public class TcpQuoteServer : IAsyncDisposable
{
    private readonly int _port;
    private readonly EventHub _eventHub;
    private readonly ILogger _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    // Карта каналов для фильтрации котировок
    private static readonly Dictionary<string, List<string>> ChannelTickers = new()
    {
        ["tech"] = new() { "GOOGL", "MSFT", "NVDA" },
        ["consumer"] = new() { "AMZN", "AAPL" },
        ["finance"] = new() { "JPM", "BAC", "GS" },
        ["energy"] = new() { "XOM", "CVX" }
    };

    public TcpQuoteServer(int port, EventHub eventHub, ILogger logger)
    {
        _port = port;
        _eventHub = eventHub;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _logger.LogInformation("TCP Quote Server listening on port {Port}", _port);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientAsync(client, _cts.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Accept failed");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        NetworkStream? stream = null;
        StreamReader? reader = null;
        StreamWriter? writer = null;

        try
        {
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = false };

            // Читаем канал (первая строка)
            var channelLine = await reader.ReadLineAsync(token);
            if (string.IsNullOrWhiteSpace(channelLine))
            {
                _logger.LogWarning("Empty channel name, closing");
                return;
            }
            var channel = channelLine.Trim().ToLowerInvariant();
            if (!ChannelTickers.ContainsKey(channel))
            {
                await writer.WriteAsync("ERROR: unknown channel\n");
                await writer.FlushAsync(token);
                return;
            }

            _eventHub.RegisterClient(channel, writer);

            // Ждём, пока соединение живо – сервер сам будет слать котировки в EventHub.PublishToChannelAsync
            // Для этого просто держим соединение открытым, пока читаем (ждём EOF)
            while (!token.IsCancellationRequested && client.Connected)
            {
                var dummy = await reader.ReadLineAsync(token);
                if (dummy == null) break; // клиент отключился
                // Можно игнорировать любые другие сообщения или обрабатывать keep-alive
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client handler error");
        }
        finally
        {
            if (reader != null) _eventHub.UnregisterClient(reader.ToString()!, writer!); // упрощённо – можно хранить пару
            try { writer?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { client.Close(); } catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_listener != null) _listener.Stop();
    }
}
