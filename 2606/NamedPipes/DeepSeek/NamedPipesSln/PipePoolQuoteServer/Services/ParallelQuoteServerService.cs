using System.Collections.Concurrent;
using QuotesServer.Helpers;
using QuotesServer.Models;
using System.Text.Json;
using QuotesServer.Services;

namespace QuotesServer.BackgroundServices;

public sealed class ParallelQuoteServerService : BackgroundService
{
    private readonly ILogger<ParallelQuoteServerService> _logger;
    private readonly EventHub _eventHub;
    private readonly ConcurrentDictionary<string, PooledPipeServer> _pipeServers = new();
    private readonly List<ChannelConfig> _channels;

    public ParallelQuoteServerService(
        ILogger<ParallelQuoteServerService> logger,
        EventHub eventHub)
    {
        _logger = logger;
        _eventHub = eventHub;

        _channels = new List<ChannelConfig>
        {
            new()
            {
                ChannelName = @"\\.\pipe\tech-stocks",
                DisplayName = "Tech Stocks",
                Tickers = new List<string> { "GOOGL", "MSFT", "NVDA" },
                MaxClients = 3,
                Color = ConsoleColor.Green
            },
            new()
            {
                ChannelName = @"\\.\pipe\consumer-stocks",
                DisplayName = "Consumer Stocks",
                Tickers = new List<string> { "AMZN", "AAPL" },
                MaxClients = 3,
                Color = ConsoleColor.Yellow
            },
            new()
            {
                ChannelName = @"\\.\pipe\finance-stocks",
                DisplayName = "Finance Stocks",
                Tickers = new List<string> { "JPM", "BAC", "GS" },
                MaxClients = 2,
                Color = ConsoleColor.Cyan
            },
            new()
            {
                ChannelName = @"\\.\pipe\energy-stocks",
                DisplayName = "Energy Stocks",
                Tickers = new List<string> { "XOM", "CVX" },
                MaxClients = 2,
                Color = ConsoleColor.Magenta
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParallelQuoteServerService: Starting with parallel channels...");

        // Запускаем все каналы параллельно
        var serverTasks = new List<Task>();

        foreach (var channel in _channels)
        {
            var server = new PooledPipeServer(
                channel.ChannelName,
                channel.DisplayName,
                channel.MaxClients,
                _eventHub,
                _logger);

            _pipeServers[channel.DisplayName] = server;

            // Запускаем каждый канал в отдельной задаче
            serverTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await server.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in channel {channel.DisplayName}");
                }
            }, stoppingToken));

            _logger.LogInformation($"Channel '{channel.DisplayName}' started with {channel.MaxClients} parallel slots");
        }

        // Настраиваем маршрутизацию
        SetupRouting();

        // Запускаем генератор котировок
        await RunQuoteGeneratorAsync(stoppingToken);
    }

    private async Task RunQuoteGeneratorAsync(CancellationToken stoppingToken)
    {
        var rnd = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);

            _eventHub.Publish(quote.Ticker, serializedData);

            // Логируем с цветом канала
            var channel = _channels.FirstOrDefault(c => c.Tickers.Contains(quote.Ticker));
            if (channel != null)
            {
                _logger.LogInformation($"📊 [{channel.DisplayName}] Published {quote.Ticker} @ {quote.Last:C}");
            }

            // Случайная задержка для более реалистичного поведения
            await Task.Delay(rnd.Next(500, 1500), stoppingToken);
        }
    }

    private void SetupRouting()
    {
        foreach (var channel in _channels)
        {
            foreach (var ticker in channel.Tickers)
            {
                _eventHub.Subscribe(ticker, async data =>
                {
                    await _eventHub.PublishToChannelAsync(channel.DisplayName, data);
                });
            }
        }

        _logger.LogInformation("ParallelQuoteServerService: Routing configured for all channels");
        foreach (var channel in _channels)
        {
            _logger.LogInformation($"{channel.DisplayName} → {string.Join(", ", channel.Tickers)} (Max {channel.MaxClients} parallel)");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ParallelQuoteServerService: Stopping...");

        foreach (var server in _pipeServers.Values)
        {
            server.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
