using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;
using QuotesServer.Interfaces;
using QuotesServer.Models;

namespace QuotesServer.Services;

public class PooledPipeServer : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _channelName;
    private readonly string _pipeName;
    private readonly int _maxClients;
    private readonly EventHub _eventHub;
    private readonly ConcurrentBag<Task> _activeConnections = new();
    private readonly SemaphoreSlim _connectionSemaphore;
    private CancellationTokenSource _cts = new();
    private bool _isDisposed;

    public string ChannelName => _channelName;
    public int ActiveConnections => _activeConnections.Count(c => !c.IsCompleted);
    public int MaxClients => _maxClients;

    public PooledPipeServer(
        string fullPipeName,
        string channelName,
        int maxClients,
        EventHub eventHub,
        ILogger logger)
    {
        _channelName = channelName;
        _pipeName = fullPipeName.Replace(@"\\.\pipe\", "");
        _maxClients = maxClients;
        _eventHub = eventHub;
        _logger = logger;
        _connectionSemaphore = new SemaphoreSlim(maxClients, maxClients);
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"PooledPipeServer [{_channelName}]: Starting with max {_maxClients} parallel clients");

        try
        {
            // Запускаем несколько серверов параллельно
            var serverTasks = new List<Task>();
            for (int i = 0; i < _maxClients; i++)
            {
                serverTasks.Add(RunSinglePipeServerAsync(i, stoppingToken));
            }

            await Task.WhenAll(serverTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"PooledPipeServer [{_channelName}]: Shutting down...");
        }
    }

    private async Task RunSinglePipeServerAsync(int serverId, CancellationToken stoppingToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token);

        while (!linkedCts.Token.IsCancellationRequested)
        {
            NamedPipeServerStream? pipeServer = null;
            ClientConnection? client = null;

            try
            {
                // Ожидаем доступный слот в семафоре
                await _connectionSemaphore.WaitAsync(linkedCts.Token);

                pipeServer = new NamedPipeServerStream(
                    $"{_pipeName}_{serverId}", // Уникальное имя для каждого экземпляра
                    PipeDirection.Out,
                    _maxClients,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogDebug($"PooledPipeServer [{_channelName}]: Server {serverId} waiting for connection...");

                await pipeServer.WaitForConnectionAsync(linkedCts.Token);

                client = new ClientConnection(pipeServer, $"{_channelName}_{serverId}");
                _eventHub.RegisterClient(_channelName, client);

                _logger.LogInformation($"PooledPipeServer [{_channelName}]: Client connected to server {serverId} (Active: {ActiveConnections}/{_maxClients})");

                // Мониторим соединение в отдельной задаче
                var connectionTask = MonitorClientAsync(client, serverId, linkedCts.Token);
                _activeConnections.Add(connectionTask);

                // Не ждем завершения, чтобы принимать новые подключения
                _ = connectionTask.ContinueWith(t =>
                {
                    _connectionSemaphore.Release();
                    _eventHub.UnregisterClient(_channelName, client);
                    client.Dispose();
                    _logger.LogInformation($"PooledPipeServer [{_channelName}]: Client disconnected from server {serverId} (Active: {ActiveConnections}/{_maxClients})");
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                _connectionSemaphore.Release();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"PooledPipeServer [{_channelName}]: Error in server {serverId}");
                _connectionSemaphore.Release();
                await Task.Delay(1000, linkedCts.Token);
            }
        }
    }

    private async Task MonitorClientAsync(ClientConnection client, int serverId, CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!client.IsConnected)
                {
                    break;
                }
                await Task.Delay(500, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _connectionSemaphore.Dispose();
    }
}
