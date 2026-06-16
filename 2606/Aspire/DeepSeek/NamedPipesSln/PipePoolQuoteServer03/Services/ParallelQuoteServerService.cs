// Services/ParallelQuoteServerService.cs

using System.Collections.Concurrent;
using QuotesServer.Helpers;
using QuotesServer.Models;
using System.Text.Json;
using QuotesServer.Services;
using System.Diagnostics;

namespace QuotesServer.BackgroundServices;

public sealed class ParallelQuoteServerService : BackgroundService
{
    private readonly ILogger<ParallelQuoteServerService> _logger;
    private readonly EventHub _eventHub;
    private readonly ConcurrentDictionary<string, PooledPipeServer> _pipeServers = new();
    private readonly List<ChannelConfig> _channels;
    private readonly Dictionary<string, int> _publishCount = new();
    private readonly string[] _allTickers; // Добавляем поле для всех тикеров

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

        // Собираем все тикеры из всех каналов
        _allTickers = _channels.SelectMany(c => c.Tickers).Distinct().ToArray();

        // Инициализируем счетчики публикаций для всех тикеров
        foreach (var ticker in _allTickers)
        {
            _publishCount[ticker] = 0;
        }

        _logger.LogInformation($"Initialized with {_allTickers.Length} tickers: {string.Join(", ", _allTickers)}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParallelQuoteServerService: Starting with parallel channels...");
        _logger.LogInformation($"Total tickers configured: {_allTickers.Length}");

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
        var statsTimer = new Stopwatch();
        statsTimer.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var quote = QuoteGenerator.Generate();
            var serializedData = JsonSerializer.Serialize(quote);

            // Увеличиваем счетчик
            if (_publishCount.ContainsKey(quote.Ticker))
            {
                _publishCount[quote.Ticker]++;
            }
            else
            {
                _logger.LogWarning($"⚠️ Unknown ticker generated: {quote.Ticker}");
                _publishCount[quote.Ticker] = 1;
            }

            _eventHub.Publish(quote.Ticker, serializedData);

            // Логируем с цветом канала
            var channel = _channels.FirstOrDefault(c => c.Tickers.Contains(quote.Ticker));
            if (channel != null)
            {
                _logger.LogInformation($"📊 [{channel.DisplayName}] Published {quote.Ticker} @ {quote.Last:C} (Total: {_publishCount[quote.Ticker]})");
            }
            else
            {
                _logger.LogWarning($"⚠️ Ticker {quote.Ticker} not assigned to any channel!");
            }

            // Каждые 30 секунд показываем статистику
            if (statsTimer.Elapsed.TotalSeconds >= 30)
            {
                LogStatistics();
                statsTimer.Restart();
            }

            // Случайная задержка
            await Task.Delay(rnd.Next(500, 1500), stoppingToken);
        }
    }

    private void LogStatistics()
    {
        _logger.LogInformation("=== Publication Statistics ===");
        foreach (var channel in _channels)
        {
            var channelStats = channel.Tickers.Select(t => $"{t}:{_publishCount.GetValueOrDefault(t, 0)}");
            _logger.LogInformation($"{channel.DisplayName}: {string.Join(", ", channelStats)}");

            // Проверяем, есть ли клиенты на канале
            var clientCount = _eventHub.GetClientCount(channel.DisplayName);
            _logger.LogInformation($"  Clients connected: {clientCount}");
        }
        _logger.LogInformation("==============================");
    }

    private void SetupRouting()
    {
        foreach (var channel in _channels)
        {
            foreach (var ticker in channel.Tickers)
            {
                _eventHub.Subscribe(ticker, async data =>
                {
                    var clientCount = _eventHub.GetClientCount(channel.DisplayName);
                    _logger.LogDebug($"Routing {ticker} to {channel.DisplayName} (clients: {clientCount})");
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
        LogStatistics();
        _logger.LogInformation("ParallelQuoteServerService: Stopping...");

        foreach (var server in _pipeServers.Values)
        {
            server.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}

