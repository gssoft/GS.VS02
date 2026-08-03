import zipfile
import os
from io import BytesIO

files = {}

# ============================================================
# 1. QuotesApp.Shared
# ============================================================

files["QuotesApp/QuotesApp.Shared/QuotesApp.Shared.csproj"] = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'''

files["QuotesApp/QuotesApp.Shared/Models/StockQuote.cs"] = '''// Models/StockQuote.cs
using System.Text.Json.Serialization;

namespace QuotesApp.Shared.Models;

public record StockQuote(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("bid")] double Bid,
    [property: JsonPropertyName("ask")] double Ask,
    [property: JsonPropertyName("last")] double Last,
    [property: JsonPropertyName("volume")] int Volume,
    [property: JsonPropertyName("portfolio")] string Portfolio,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
'''

files["QuotesApp/QuotesApp.Shared/Models/ChannelConfig.cs"] = '''// Models/ChannelConfig.cs
namespace QuotesApp.Shared.Models;

public class ChannelConfig
{
    public string ChannelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Tickers { get; set; } = new();
    public int MaxClients { get; set; } = 3;
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
}
'''

files["QuotesApp/QuotesApp.Shared/Models/PortfolioDefinition.cs"] = '''// Models/PortfolioDefinition.cs
namespace QuotesApp.Shared.Models;

public static class PortfolioDefinition
{
    public static readonly Dictionary<string, ChannelConfig> Portfolios = new()
    {
        ["tech"] = new ChannelConfig
        {
            ChannelName = "tech-stocks",
            DisplayName = "TECH STOCKS",
            Tickers = new List<string> { "GOOGL", "MSFT", "NVDA" },
            MaxClients = 3,
            Color = ConsoleColor.Green
        },
        ["consumer"] = new ChannelConfig
        {
            ChannelName = "consumer-stocks",
            DisplayName = "CONSUMER STOCKS",
            Tickers = new List<string> { "AMZN", "AAPL" },
            MaxClients = 3,
            Color = ConsoleColor.Yellow
        },
        ["finance"] = new ChannelConfig
        {
            ChannelName = "finance-stocks",
            DisplayName = "FINANCE STOCKS",
            Tickers = new List<string> { "JPM", "BAC", "GS" },
            MaxClients = 3,
            Color = ConsoleColor.Cyan
        },
        ["energy"] = new ChannelConfig
        {
            ChannelName = "energy-stocks",
            DisplayName = "ENERGY STOCKS",
            Tickers = new List<string> { "XOM", "CVX" },
            MaxClients = 3,
            Color = ConsoleColor.Red
        }
    };
}
'''

# ============================================================
# 2. QuotesApp.ServiceDefaults
# ============================================================

files["QuotesApp/QuotesApp.ServiceDefaults/QuotesApp.ServiceDefaults.csproj"] = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.4.0" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.2.1" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
  </ItemGroup>
</Project>
'''

files["QuotesApp/QuotesApp.ServiceDefaults/Extensions.cs"] = '''// Extensions.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        return app;
    }
}
'''

# ============================================================
# 3. QuotesApp.PipeServer (Worker Service)
# ============================================================

files["QuotesApp/QuotesApp.PipeServer/QuotesApp.PipeServer.csproj"] = '''<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\QuotesApp.Shared\\QuotesApp.Shared.csproj" />
    <ProjectReference Include="..\\QuotesApp.ServiceDefaults\\QuotesApp.ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.7" />
    <PackageReference Include="Aspire.StackExchange.Redis" Version="13.2.4" />
  </ItemGroup>
</Project>
'''

files["QuotesApp/QuotesApp.PipeServer/Program.cs"] = '''// Program.cs
using QuotesApp.PipeServer.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");

builder.Services.AddHostedService<QuotePublisherService>();

var host = builder.Build();
host.Run();
'''

files["QuotesApp/QuotesApp.PipeServer/appsettings.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "QuoteSettings": {
    "IntervalMs": 1000
  }
}
'''

files["QuotesApp/QuotesApp.PipeServer/appsettings.Development.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
'''

files["QuotesApp/QuotesApp.PipeServer/Services/QuoteGenerator.cs"] = '''// Services/QuoteGenerator.cs
using QuotesApp.Shared.Models;

namespace QuotesApp.PipeServer.Services;

public static class QuoteGenerator
{
    private static readonly Random _rnd = new();

    private static readonly Dictionary<string, double> BasePrices = new()
    {
        ["GOOGL"] = 178.50, ["MSFT"] = 425.30, ["NVDA"] = 890.75,
        ["AMZN"] = 185.20, ["AAPL"] = 232.40,
        ["JPM"] = 205.60, ["BAC"] = 42.80, ["GS"] = 478.90,
        ["XOM"] = 118.30, ["CVX"] = 162.50
    };

    public static StockQuote Generate(string ticker, string portfolio)
    {
        var basePrice = BasePrices.GetValueOrDefault(ticker, 100.0);
        var change = (_rnd.NextDouble() - 0.5) * basePrice * 0.02;
        var newPrice = basePrice + change;
        var spread = Math.Round(newPrice * 0.001, 2);

        return new StockQuote(
            Ticker: ticker,
            Bid: Math.Round(newPrice - spread, 2),
            Ask: Math.Round(newPrice + spread, 2),
            Last: Math.Round(newPrice, 2),
            Volume: _rnd.Next(100, 50000),
            Portfolio: portfolio,
            Timestamp: DateTime.UtcNow
        );
    }
}
'''

files["QuotesApp/QuotesApp.PipeServer/Services/QuotePublisherService.cs"] = '''// Services/QuotePublisherService.cs
using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

namespace QuotesApp.PipeServer.Services;

public class QuotePublisherService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QuotePublisherService> _logger;
    private readonly int _intervalMs;

    public QuotePublisherService(
        IConnectionMultiplexer redis,
        ILogger<QuotePublisherService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _intervalMs = configuration.GetValue<int>("QuoteSettings:IntervalMs", 1000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuotePublisherService started. Interval: {Interval}ms", _intervalMs);

        var subscriber = _redis.GetSubscriber();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var (portfolioKey, config) in PortfolioDefinition.Portfolios)
                {
                    foreach (var ticker in config.Tickers)
                    {
                        var quote = QuoteGenerator.Generate(ticker, portfolioKey);
                        var json = JsonSerializer.Serialize(quote);

                        // Publish to portfolio-specific channel
                        await subscriber.PublishAsync(
                            RedisChannel.Literal($"quotes:{portfolioKey}"), json);

                        // Publish to global dashboard channel
                        await subscriber.PublishAsync(
                            RedisChannel.Literal("quotes:all"), json);

                        // Store latest in Redis Hash
                        await _redis.GetDatabase().HashSetAsync(
                            $"dashboard:{portfolioKey}", ticker, json);
                    }
                }

                _logger.LogDebug("Published quotes for all portfolios");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error publishing quotes");
            }

            await Task.Delay(_intervalMs, stoppingToken);
        }
    }
}
'''

# ============================================================
# 4. QuotesApp.ApiService
# ============================================================

files["QuotesApp/QuotesApp.ApiService/QuotesApp.ApiService.csproj"] = '''<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\QuotesApp.Shared\\QuotesApp.Shared.csproj" />
    <ProjectReference Include="..\\QuotesApp.ServiceDefaults\\QuotesApp.ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.StackExchange.Redis" Version="13.2.4" />
  </ItemGroup>
</Project>
'''

files["QuotesApp/QuotesApp.ApiService/Program.cs"] = '''// Program.cs
using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// GET /quotes — all latest quotes
app.MapGet("/quotes", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var quotes = new List<StockQuote>();

    foreach (var portfolio in PortfolioDefinition.Portfolios.Keys)
    {
        var entries = await db.HashGetAllAsync($"dashboard:{portfolio}");
        foreach (var entry in entries)
        {
            var q = JsonSerializer.Deserialize<StockQuote>(entry.Value!);
            if (q is not null) quotes.Add(q);
        }
    }

    return Results.Ok(quotes);
})
.WithName("GetAllQuotes")
.WithOpenApi();

// GET /quotes/{portfolio} — quotes by portfolio
app.MapGet("/quotes/{portfolio}", async (string portfolio, IConnectionMultiplexer redis) =>
{
    if (!PortfolioDefinition.Portfolios.ContainsKey(portfolio))
        return Results.NotFound($"Portfolio \'{portfolio}\' not found.");

    var db = redis.GetDatabase();
    var entries = await db.HashGetAllAsync($"dashboard:{portfolio}");
    var quotes = entries
        .Select(e => JsonSerializer.Deserialize<StockQuote>(e.Value!))
        .Where(q => q is not null)
        .ToList();

    return Results.Ok(quotes);
})
.WithName("GetQuotesByPortfolio")
.WithOpenApi();

// GET /portfolios — list available portfolios
app.MapGet("/portfolios", () =>
{
    var list = PortfolioDefinition.Portfolios
        .Select(p => new { p.Key, p.Value.DisplayName, Tickers = p.Value.Tickers })
        .ToList();
    return Results.Ok(list);
})
.WithName("GetPortfolios")
.WithOpenApi();

app.MapDefaultEndpoints();
app.Run();
'''

files["QuotesApp/QuotesApp.ApiService/appsettings.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'''

files["QuotesApp/QuotesApp.ApiService/appsettings.Development.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
'''

files["QuotesApp/QuotesApp.ApiService/Properties/launchSettings.json"] = '''{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7401;http://localhost:5401",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
// '''
/*
# ============================================================
# 5. QuotesApp.Web (Blazor Server)
# ============================================================
*/
/*
files["QuotesApp/QuotesApp.Web/QuotesApp.Web.csproj"] = '''<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\QuotesApp.Shared\\QuotesApp.Shared.csproj" />
    <ProjectReference Include="..\\QuotesApp.ServiceDefaults\\QuotesApp.ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.StackExchange.Redis" Version="13.2.4" />
  </ItemGroup>
</Project>

*/
//'''

files["QuotesApp/QuotesApp.Web/Program.cs"] = '''// Program.cs
using QuotesApp.Web;
using QuotesApp.Web.Components;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<QuotesApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
});

builder.Services.AddSingleton<QuoteStreamService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.Run();
'''

files["QuotesApp/QuotesApp.Web/QuotesApiClient.cs"] = '''// QuotesApiClient.cs
using System.Net.Http.Json;
using QuotesApp.Shared.Models;

namespace QuotesApp.Web;

public class QuotesApiClient(HttpClient httpClient)
{
    public async Task<List<StockQuote>> GetQuotesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<StockQuote>>("/quotes", ct)
            ?? new List<StockQuote>();
    }

    public async Task<List<StockQuote>> GetQuotesByPortfolioAsync(
        string portfolio, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<StockQuote>>($"/quotes/{portfolio}", ct)
            ?? new List<StockQuote>();
    }
}
'''

files["QuotesApp/QuotesApp.Web/QuoteStreamService.cs"] = '''// QuoteStreamService.cs
using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

namespace QuotesApp.Web;

public class QuoteStreamService : IAsyncDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QuoteStreamService> _logger;
    private ISubscriber? _subscriber;

    public event Func<StockQuote, Task>? OnQuoteReceived;

    public QuoteStreamService(IConnectionMultiplexer redis, ILogger<QuoteStreamService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _subscriber = _redis.GetSubscriber();

        await _subscriber.SubscribeAsync(
            RedisChannel.Literal("quotes:all"),
            async (channel, message) =>
            {
                try
                {
                    var quote = JsonSerializer.Deserialize<StockQuote>(message!);
                    if (quote is not null && OnQuoteReceived is not null)
                    {
                        await OnQuoteReceived.Invoke(quote);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing quote from Redis");
                }
            });

        _logger.LogInformation("Subscribed to quotes:all channel");
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscriber is not null)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal("quotes:all"));
        }
    }
}
'''

files["QuotesApp/QuotesApp.Web/appsettings.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'''

files["QuotesApp/QuotesApp.Web/appsettings.Development.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
'''

files["QuotesApp/QuotesApp.Web/Properties/launchSettings.json"] = '''{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7501;http://localhost:5501",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
'''

# Blazor Components
files["QuotesApp/QuotesApp.Web/Components/App.razor"] = '''<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
'''

files["QuotesApp/QuotesApp.Web/Components/Routes.razor"] = '''<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
'''

files["QuotesApp/QuotesApp.Web/Components/_Imports.razor"] = '''@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using QuotesApp.Web
@using QuotesApp.Web.Components
@using QuotesApp.Shared.Models
'''

files["QuotesApp/QuotesApp.Web/Components/Layout/MainLayout.razor"] = '''@inherits LayoutComponentBase

<div class="page">
    <main>
        @Body
    </main>
</div>
'''

files["QuotesApp/QuotesApp.Web/Components/Pages/Home.razor"] = '''@page "/"
@using QuotesApp.Web
@using QuotesApp.Shared.Models
@inject QuotesApiClient ApiClient
@inject QuoteStreamService StreamService
@inject ILogger<Home> Logger
@rendermode InteractiveServer
@implements IAsyncDisposable

<PageTitle>QuotesApp — Котировки в реальном времени</PageTitle>

<div class="container mt-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="mb-0">
                📈 Котировки акций
                @if (_autoRefresh)
                {
                    <span class="badge bg-danger ms-2 live-badge">
                        <span class="live-dot"></span> LIVE
                    </span>
                }
            </h1>
            @if (_lastUpdate != default)
            {
                <small class="text-muted">🕐 Обновлено: @_lastUpdate.ToString("HH:mm:ss")</small>
            }
        </div>
        <div class="d-flex gap-2 align-items-center">
            <!-- Auto-refresh toggle -->
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="autoRefresh"
                       checked="@_autoRefresh" @onchange="ToggleAutoRefresh" />
                <label class="form-check-label" for="autoRefresh">Авто</label>
            </div>
            <!-- Interval selector -->
            <select class="form-select form-select-sm" style="width:80px"
                    @onchange="OnIntervalChanged" disabled="@(!_autoRefresh)">
                <option value="3">3с</option>
                <option value="5">5с</option>
                <option value="10" selected>10с</option>
                <option value="30">30с</option>
                <option value="60">60с</option>
            </select>
            <!-- Portfolio filter -->
            <select class="form-select form-select-sm" style="width:140px"
                    @onchange="OnPortfolioChanged">
                <option value="">Все портфели</option>
                <option value="tech">🟢 Tech</option>
                <option value="consumer">🟡 Consumer</option>
                <option value="finance">🔵 Finance</option>
                <option value="energy">🔴 Energy</option>
            </select>
            <button class="btn btn-sm btn-outline-primary" @onclick="ManualRefreshAsync">
                🔄 Обновить
            </button>
        </div>
    </div>

    <!-- Loading -->
    @if (_isLoading && _quotes.Count == 0)
    {
        <div class="alert alert-info">
            <span class="spinner-border spinner-border-sm me-2"></span>
            Загрузка котировок из ApiService...
        </div>
    }
    <!-- Error -->
    else if (_error is not null)
    {
        <div class="alert alert-danger">
            <strong>Ошибка:</strong> @_error
            @if (_autoRefresh)
            {
                <small class="d-block mt-1">Автоповтор через @_intervalSec сек...</small>
            }
        </div>
    }
    <!-- Quotes Table -->
    else if (_filteredQuotes.Count > 0)
    {
        <div class="table-responsive">
            <table class="table table-dark table-hover table-striped align-middle">
                <thead>
                    <tr>
                        <th>Тикер</th>
                        <th>Портфель</th>
                        <th class="text-end">Bid</th>
                        <th class="text-end">Ask</th>
                        <th class="text-end">Last</th>
                        <th class="text-end">Spread</th>
                        <th class="text-end">Volume</th>
                        <th class="text-end">Время</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var q in _filteredQuotes)
                    {
                        <tr class="@GetRowClass(q)">
                            <td><strong>@q.Ticker</strong></td>
                            <td>
                                <span class="badge @GetPortfolioBadge(q.Portfolio)">
                                    @q.Portfolio.ToUpper()
                                </span>
                            </td>
                            <td class="text-end text-success">@q.Bid.ToString("F2")</td>
                            <td class="text-end text-danger">@q.Ask.ToString("F2")</td>
                            <td class="text-end fw-bold">@q.Last.ToString("F2")</td>
                            <td class="text-end text-muted">@((q.Ask - q.Bid).ToString("F2"))</td>
                            <td class="text-end">@q.Volume.ToString("N0")</td>
                            <td class="text-end"><small>@q.Timestamp.ToString("HH:mm:ss")</small></td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        <div class="text-muted small">
            Всего: @_filteredQuotes.Count котировок
            @if (!string.IsNullOrEmpty(_selectedPortfolio))
            {
                <span>(фильтр: @_selectedPortfolio)</span>
            }
        </div>
    }
    else
    {
        <div class="alert alert-warning">Нет данных для отображения.</div>
    }
</div>

<style>
    .live-badge { animation: pulse 1.5s infinite; }
    .live-dot {
        display: inline-block;
        width: 8px; height: 8px;
        background: white;
        border-radius: 50%;
        margin-right: 4px;
        animation: blink 1s infinite;
    }
    @@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.7} }
    @@keyframes blink { 0%,100%{opacity:1} 50%{opacity:0.3} }
    .quote-up { background-color: rgba(25, 135, 84, 0.1) !important; }
    .quote-down { background-color: rgba(220, 53, 69, 0.1) !important; }
</style>

@code {
    private List<StockQuote> _quotes = new();
    private string? _error;
    private bool _isLoading = true;
    private bool _autoRefresh = true;
    private int _intervalSec = 10;
    private DateTime _lastUpdate;
    private string _selectedPortfolio = "";
    private System.Threading.Timer? _timer;
    private readonly Dictionary<string, double> _prevPrices = new();

    private List<StockQuote> _filteredQuotes =>
        string.IsNullOrEmpty(_selectedPortfolio)
            ? _quotes
            : _quotes.Where(q => q.Portfolio == _selectedPortfolio).ToList();

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to real-time stream
        StreamService.OnQuoteReceived += OnRealTimeQuote;
        await StreamService.StartAsync();

        await ManualRefreshAsync();
        StartAutoRefresh();
    }

    private async Task OnRealTimeQuote(StockQuote quote)
    {
        await InvokeAsync(() =>
        {
            var existing = _quotes.FindIndex(q => q.Ticker == quote.Ticker);
            if (existing >= 0)
                _quotes[existing] = quote;
            else
                _quotes.Add(quote);

            _lastUpdate = DateTime.Now;
            StateHasChanged();
        });
    }

    private async Task ManualRefreshAsync()
    {
        try
        {
            _error = null;
            var data = string.IsNullOrEmpty(_selectedPortfolio)
                ? await ApiClient.GetQuotesAsync()
                : await ApiClient.GetQuotesByPortfolioAsync(_selectedPortfolio);

            foreach (var q in data)
            {
                _prevPrices[q.Ticker] = _quotes
                    .FirstOrDefault(x => x.Ticker == q.Ticker)?.Last ?? q.Last;
            }

            _quotes = data;
            _lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            Logger.LogError(ex, "Failed to fetch quotes");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void StartAutoRefresh()
    {
        _timer?.Dispose();
        _timer = new System.Threading.Timer(
            async _ => await InvokeAsync(ManualRefreshAsync),
            null,
            TimeSpan.FromSeconds(_intervalSec),
            TimeSpan.FromSeconds(_intervalSec));
    }

    private void StopAutoRefresh()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void ToggleAutoRefresh()
    {
        _autoRefresh = !_autoRefresh;
        if (_autoRefresh) StartAutoRefresh();
        else StopAutoRefresh();
    }

    private void OnIntervalChanged(ChangeEventArgs e)
    {
        _intervalSec = int.Parse(e.Value?.ToString() ?? "10");
        if (_autoRefresh) StartAutoRefresh();
    }

    private async Task OnPortfolioChanged(ChangeEventArgs e)
    {
        _selectedPortfolio = e.Value?.ToString() ?? "";
        await ManualRefreshAsync();
    }

    private string GetRowClass(StockQuote q)
    {
        if (_prevPrices.TryGetValue(q.Ticker, out var prev))
        {
            if (q.Last > prev) return "quote-up";
            if (q.Last < prev) return "quote-down";
        }
        return "";
    }

    private string GetPortfolioBadge(string portfolio) => portfolio switch
    {
        "tech" => "bg-success",
        "consumer" => "bg-warning text-dark",
        "finance" => "bg-info",
        "energy" => "bg-danger",
        _ => "bg-secondary"
    };

    public async ValueTask DisposeAsync()
    {
        StopAutoRefresh();
        StreamService.OnQuoteReceived -= OnRealTimeQuote;
        await ValueTask.CompletedTask;
    }
}
'''

# ============================================================
# 6. QuotesApp.AppHost (Aspire Orchestrator)
# ============================================================

files["QuotesApp/QuotesApp.AppHost/QuotesApp.AppHost.csproj"] = '''<Project Sdk="Aspire.AppHost.Sdk/13.4.6">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UserSecretsId>a1b2c3d4-e5f6-7890-abcd-ef1234567890</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\QuotesApp.PipeServer\\QuotesApp.PipeServer.csproj" />
    <ProjectReference Include="..\\QuotesApp.ApiService\\QuotesApp.ApiService.csproj" />
    <ProjectReference Include="..\\QuotesApp.Web\\QuotesApp.Web.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="13.4.6" />
    <PackageReference Include="Aspire.Hosting.Redis" Version="13.4.6" />
  </ItemGroup>
</Project>
'''

files["QuotesApp/QuotesApp.AppHost/Program.cs"] = '''// Program.cs — Aspire AppHost Orchestrator
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("cache");

// Worker: generates and publishes quotes
var pipeServer = builder.AddProject<Projects.QuotesApp_PipeServer>("pipeserver")
    .WithReference(redis)
    .WaitFor(redis);

// API: REST endpoints
var apiService = builder.AddProject<Projects.QuotesApp_ApiService>("apiservice")
    .WithReference(redis)
    .WaitFor(redis);

// Web: Blazor frontend
builder.AddProject<Projects.QuotesApp_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(redis)
    .WaitFor(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
'''

files["QuotesApp/QuotesApp.AppHost/appsettings.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  }
}
'''

files["QuotesApp/QuotesApp.AppHost/appsettings.Development.json"] = '''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
'''

files["QuotesApp/QuotesApp.AppHost/Properties/launchSettings.json"] = '''{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17888;http://localhost:15888",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21072",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22000"
      }
    }
  }
}
'''

# ============================================================
# 7. Solution file
# ============================================================

files["QuotesApp/QuotesApp.sln"] = '''
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.12.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.AppHost", "QuotesApp.AppHost\\QuotesApp.AppHost.csproj", "{A1111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.Shared", "QuotesApp.Shared\\QuotesApp.Shared.csproj", "{B2222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.ServiceDefaults", "QuotesApp.ServiceDefaults\\QuotesApp.ServiceDefaults.csproj", "{C3333333-3333-3333-3333-333333333333}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.PipeServer", "QuotesApp.PipeServer\\QuotesApp.PipeServer.csproj", "{D4444444-4444-4444-4444-444444444444}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.ApiService", "QuotesApp.ApiService\\QuotesApp.ApiService.csproj", "{E5555555-5555-5555-5555-555555555555}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.Web", "QuotesApp.Web\\QuotesApp.Web.csproj", "{F6666666-6666-6666-6666-666666666666}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1111111-1111-1111-1111-111111111111}.Release|Any CPU.Build.0 = Release|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B2222222-2222-2222-2222-222222222222}.Release|Any CPU.Build.0 = Release|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C3333333-3333-3333-3333-333333333333}.Release|Any CPU.Build.0 = Release|Any CPU
		{D4444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D4444444-4444-4444-4444-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D4444444-4444-4444-4444-444444444444}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D4444444-4444-4444-4444-444444444444}.Release|Any CPU.Build.0 = Release|Any CPU
		{E5555555-5555-5555-5555-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E5555555-5555-5555-5555-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E5555555-5555-5555-5555-555555555555}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{E5555555-5555-5555-5555-555555555555}.Release|Any CPU.Build.0 = Release|Any CPU
		{F6666666-6666-6666-6666-666666666666}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F6666666-6666-6666-6666-666666666666}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F6666666-6666-6666-6666-666666666666}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F6666666-6666-6666-6666-666666666666}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
'''

# README
files["QuotesApp/README.md"] = '''# QuotesApp — Real-time Stock Quotes (.NET Aspire)

## Architecture
