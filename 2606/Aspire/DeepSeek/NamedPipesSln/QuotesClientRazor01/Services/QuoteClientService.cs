// Services/QuoteClientService.cs

using System.IO.Pipes;
using System.Text.Json;
using QuotesClientRazor01.Models;

namespace QuotesClientRazor01.Services;

public class QuoteClientService : BackgroundService
{
    private readonly ILogger<QuoteClientService> _logger;
    private readonly QuoteCache _quoteCache;
    private readonly IConfiguration _configuration;

    private string _channel = "finance"; // По умолчанию
    private string _pipeBaseName = "finance-stocks";
    private List<string> _expectedTickers = new() { "JPM", "BAC", "GS" };

    public QuoteClientService(
        ILogger<QuoteClientService> logger,
        QuoteCache quoteCache,
        IConfiguration configuration)
    {
        _logger = logger;
        _quoteCache = quoteCache;
        _configuration = configuration;

        // Загружаем настройки из конфигурации
        _channel = _configuration.GetValue<string>("QuoteChannel", "finance");

        // Настраиваем ожидаемые тикеры в зависимости от канала
        ConfigureChannel(_channel);
    }

    private void ConfigureChannel(string channel)
    {
        switch (channel.ToLower())
        {
            case "tech":
                _pipeBaseName = "tech-stocks";
                _expectedTickers = new List<string> { "GOOGL", "MSFT", "NVDA" };
                break;
            case "consumer":
                _pipeBaseName = "consumer-stocks";
                _expectedTickers = new List<string> { "AMZN", "AAPL" };
                break;
            case "finance":
                _pipeBaseName = "finance-stocks";
                _expectedTickers = new List<string> { "JPM", "BAC", "GS" };
                break;
            case "energy":
                _pipeBaseName = "energy-stocks";
                _expectedTickers = new List<string> { "XOM", "CVX" };
                break;
        }

        // Инициализируем кэш с ожидаемыми тикерами
        _quoteCache.InitializeTickers(_expectedTickers);

        _logger.LogInformation($"Configured for channel: {channel}, tickers: {string.Join(", ", _expectedTickers)}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"QuoteClientService: Starting for channel {_channel}...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ConnectAndReceiveAsync(stoppingToken);
        }
    }

    private async Task ConnectAndReceiveAsync(CancellationToken stoppingToken)
    {
        NamedPipeClientStream? pipeClient = null;
        StreamReader? reader = null;

        try
        {
            // Пытаемся подключиться к любому доступному экземпляру сервера
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var pipeName = $"{_pipeBaseName}_{i}";
                    _logger.LogDebug($"Attempting to connect to {pipeName}...");

                    pipeClient = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    await pipeClient.ConnectAsync(2000, stoppingToken);
                    _logger.LogInformation($"✅ Connected to {pipeName}!");
                    break;
                }
                catch (TimeoutException)
                {
                    _logger.LogDebug($"Timeout on instance {i}, trying next...");
                    continue;
                }
            }

            if (pipeClient == null || !pipeClient.IsConnected)
            {
                throw new TimeoutException("No available server instances");
            }

            reader = new StreamReader(pipeClient);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!pipeClient.IsConnected)
                {
                    _logger.LogWarning("Connection lost to server");
                    break;
                }

                var line = await reader.ReadLineAsync(stoppingToken);
                if (line == null)
                {
                    _logger.LogWarning("Server disconnected");
                    break;
                }

                try
                {
                    var quote = JsonSerializer.Deserialize<StockQuote>(line);
                    if (quote != null && _expectedTickers.Contains(quote.Ticker))
                    {
                        _quoteCache.UpdateQuote(quote);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("QuoteClientService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QuoteClientService, reconnecting in 5s...");
            await Task.Delay(5000, stoppingToken);
        }
        finally
        {
            try { reader?.Dispose(); } catch { }
            try { pipeClient?.Dispose(); } catch { }
        }
    }
}
