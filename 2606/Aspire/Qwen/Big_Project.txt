import os

# Создаем структуру папок
directories = [
    "QuotesApp.Shared",
    "QuotesApp.PipeServer",
    "QuotesApp.ApiService",
    "QuotesApp.Web",
    "QuotesApp.ServiceDefaults",
    "QuotesApp.AppHost"
]

for dir_name in directories:
    os.makedirs(dir_name, exist_ok=True)
    print(f"✅ Создана папка: {dir_name}")

 # Result 
✅ Создана папка: QuotesApp.Shared
✅ Создана папка: QuotesApp.PipeServer
✅ Создана папка: QuotesApp.ApiService
✅ Создана папка: QuotesApp.Web
✅ Создана папка: QuotesApp.ServiceDefaults
✅ Создана папка: QuotesApp.AppHost
----------------------------------------------------------------------------
# QuotesApp.Shared/QuotesApp.Shared.csproj
shared_csproj = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
"""

with open("QuotesApp.Shared/QuotesApp.Shared.csproj", "w", encoding="utf-8") as f:
    f.write(shared_csproj)

print("✅ Создан файл: QuotesApp.Shared/QuotesApp.Shared.csproj")

 # Result 
✅ Создан файл: QuotesApp.Shared/QuotesApp.Shared.csproj
------------------------------------------------------------------------------
# QuotesApp.Shared/Models/StockQuote.cs
os.makedirs("QuotesApp.Shared/Models", exist_ok=True)

stock_quote_cs = """namespace QuotesApp.Shared.Models;

public sealed record StockQuote(
    string Ticker,
    decimal Bid,
    decimal Ask,
    decimal Last,
    int Volume,
    string Portfolio,
    DateTimeOffset Timestamp
);
"""

with open("QuotesApp.Shared/Models/StockQuote.cs", "w", encoding="utf-8") as f:
    f.write(stock_quote_cs)

print("✅ Создан файл: QuotesApp.Shared/Models/StockQuote.cs")

 # Result 
✅ Создан файл: QuotesApp.Shared/Models/StockQuote.cs
--------------------------------------------------------
# QuotesApp.PipeServer/QuotesApp.PipeServer.csproj
pipe_server_csproj = """<Project Sdk="Microsoft.NET.Sdk.Worker">
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
"""

with open("QuotesApp.PipeServer/QuotesApp.PipeServer.csproj", "w", encoding="utf-8") as f:
    f.write(pipe_server_csproj)

print("✅ Создан файл: QuotesApp.PipeServer/QuotesApp.PipeServer.csproj")

 # Result 
✅ Создан файл: QuotesApp.PipeServer/QuotesApp.PipeServer.csproj
--------------------------------------------------
# QuotesApp.PipeServer/Services/QuoteGenerator.cs
os.makedirs("QuotesApp.PipeServer/Services", exist_ok=True)

quote_generator_cs = """using QuotesApp.Shared.Models;

namespace QuotesApp.PipeServer.Services;

public class QuoteGenerator
{
    private static readonly Random _rnd = new();
    
    private static readonly Dictionary<string, string[]> _portfolios = new()
    {
        ["Tech"] = ["GOOGL", "MSFT", "NVDA"],
        ["Consumer"] = ["AMZN", "AAPL"],
        ["Finance"] = ["JPM", "BAC", "GS"],
        ["Energy"] = ["XOM", "CVX"]
    };

    public static StockQuote Generate()
    {
        var portfolio = _portfolios.Keys.ElementAt(_rnd.Next(_portfolios.Keys.Count));
        var tickers = _portfolios[portfolio];
        var ticker = tickers[_rnd.Next(tickers.Length)];
        
        decimal basePrice = ticker switch
        {
            "GOOGL" or "AMZN" or "NVDA" => (decimal)(_rnd.Next(1000, 3000) + _rnd.NextDouble()),
            "JPM" or "GS" => (decimal)(_rnd.Next(200, 500) + _rnd.NextDouble()),
            _ => (decimal)(_rnd.Next(50, 500) + _rnd.NextDouble())
        };

        var spread = (decimal)(_rnd.NextDouble() * 2);
        
        return new StockQuote(
            Ticker: ticker,
            Bid: Math.Round(basePrice - spread, 2),
            Ask: Math.Round(basePrice + spread, 2),
            Last: Math.Round(basePrice, 2),
            Volume: _rnd.Next(1, 10000),
            Portfolio: portfolio,
            Timestamp: DateTimeOffset.UtcNow
        );
    }
}
"""

with open("QuotesApp.PipeServer/Services/QuoteGenerator.cs", "w", encoding="utf-8") as f:
    f.write(quote_generator_cs)

print("✅ Создан файл: QuotesApp.PipeServer/Services/QuoteGenerator.cs")

 # Result 
✅ Создан файл: QuotesApp.PipeServer/Services/QuoteGenerator.cs
-------------------
# QuotesApp.PipeServer/Services/QuotePublisherService.cs
quote_publisher_cs = """using System.Text.Json;
using QuotesApp.Shared.Models;
using StackExchange.Redis;

namespace QuotesApp.PipeServer.Services;

public class QuotePublisherService : BackgroundService
{
    private readonly ILogger<QuotePublisherService> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly Random _rnd = new();

    public QuotePublisherService(
        ILogger<QuotePublisherService> logger,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuotePublisherService запущен. Начинаю генерацию котировок...");
        
        var subscriber = _redis.GetSubscriber();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var quote = QuoteGenerator.Generate();
                var json = JsonSerializer.Serialize(quote);
                
                // Публикуем в канал для конкретного портфеля
                await subscriber.PublishAsync(
                    RedisChannel.Literal($"quotes:{quote.Portfolio.ToLower()}"),
                    json
                );
                
                // Также публикуем в общий канал для всех котировок
                await subscriber.PublishAsync(
                    RedisChannel.Literal("quotes:all"),
                    json
                );
                
                _logger.LogDebug($"📊 Опубликована котировка: {quote.Ticker} @ {quote.Last:C} (Портфель: {quote.Portfolio})");
                
                // Случайная задержка между публикациями
                await Task.Delay(_rnd.Next(500, 2000), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при публикации котировки");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
"""

with open("QuotesApp.PipeServer/Services/QuotePublisherService.cs", "w", encoding="utf-8") as f:
    f.write(quote_publisher_cs)

print("✅ Создан файл: QuotesApp.PipeServer/Services/QuotePublisherService.cs")

 # Result 
✅ Создан файл: QuotesApp.PipeServer/Services/QuotePublisherService.cs
---------------------------------------------------
# QuotesApp.PipeServer/Program.cs
pipe_server_program = """using QuotesApp.PipeServer.Services;

var builder = Host.CreateApplicationBuilder(args);

// Добавляем Service Defaults от Aspire
builder.AddServiceDefaults();

// Добавляем Redis клиент
builder.AddRedisClient("cache");

// Регистрируем сервис публикации котировок
builder.Services.AddHostedService<QuotePublisherService>();

var host = builder.Build();
host.Run();
"""

with open("QuotesApp.PipeServer/Program.cs", "w", encoding="utf-8") as f:
    f.write(pipe_server_program)

# appsettings.json
appsettings = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
"""

with open("QuotesApp.PipeServer/appsettings.json", "w", encoding="utf-8") as f:
    f.write(appsettings)

print("✅ Создан файл: QuotesApp.PipeServer/Program.cs")
print("✅ Создан файл: QuotesApp.PipeServer/appsettings.json")

 # Result 
✅ Создан файл: QuotesApp.PipeServer/Program.cs
✅ Создан файл: QuotesApp.PipeServer/appsettings.json
------------
# QuotesApp.ServiceDefaults/QuotesApp.ServiceDefaults.csproj
service_defaults_csproj = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.8.0" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.8.0" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.17.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.17.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.17.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.17.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.17.0" />
  </ItemGroup>
</Project>
"""

with open("QuotesApp.ServiceDefaults/QuotesApp.ServiceDefaults.csproj", "w", encoding="utf-8") as f:
    f.write(service_defaults_csproj)

print("✅ Создан файл: QuotesApp.ServiceDefaults/QuotesApp.ServiceDefaults.csproj")

 # Result 
✅ Создан файл: QuotesApp.ServiceDefaults/QuotesApp.ServiceDefaults.csproj
---------------------------------------------------
# QuotesApp.ServiceDefaults/Extensions.cs
extensions_cs = """using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
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

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
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
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }
        return app;
    }
}
"""

with open("QuotesApp.ServiceDefaults/Extensions.cs", "w", encoding="utf-8") as f:
    f.write(extensions_cs)

print("✅ Создан файл: QuotesApp.ServiceDefaults/Extensions.cs")

 # Result 
✅ Создан файл: QuotesApp.ServiceDefaults/Extensions.cs
------------------------------
# QuotesApp.ApiService/QuotesApp.ApiService.csproj
api_service_csproj = """<Project Sdk="Microsoft.NET.Sdk.Web">
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
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.10" />
  </ItemGroup>
</Project>
"""

with open("QuotesApp.ApiService/QuotesApp.ApiService.csproj", "w", encoding="utf-8") as f:
    f.write(api_service_csproj)

print("✅ Создан файл: QuotesApp.ApiService/QuotesApp.ApiService.csproj")

 # Result 
✅ Создан файл: QuotesApp.ApiService/QuotesApp.ApiService.csproj
---------------------------------
# QuotesApp.ApiService/Program.cs
api_program = """using System.Text.Json;
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

app.MapGet("/", () => "QuotesApp API Service is running. Navigate to /quotes to see data.");

// Endpoint для получения всех котировок
app.MapGet("/quotes", async (IConnectionMultiplexer redis) =>
{
    var subscriber = redis.GetSubscriber();
    var db = redis.GetDatabase();
    
    // Получаем последние котировки из Redis (храним в hash)
    var quotes = new List<StockQuote>();
    
    var portfolios = new[] { "tech", "consumer", "finance", "energy" };
    foreach (var portfolio in portfolios)
    {
        var key = $"latest_quotes:{portfolio}";
        var latestJson = await db.StringGetAsync(key);
        if (latestJson.HasValue)
        {
            var quote = JsonSerializer.Deserialize<StockQuote>(latestJson!);
            if (quote != null)
            {
                quotes.Add(quote);
            }
        }
    }
    
    return quotes;
})
.WithName("GetAllQuotes");

// Endpoint для получения котировок по портфелю
app.MapGet("/quotes/{portfolio}", async (string portfolio, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var key = $"latest_quotes:{portfolio.ToLower()}";
    var latestJson = await db.StringGetAsync(key);
    
    if (latestJson.HasValue)
    {
        var quote = JsonSerializer.Deserialize<StockQuote>(latestJson!);
        return quote;
    }
    
    return null;
})
.WithName("GetQuotesByPortfolio");

app.MapDefaultEndpoints();
app.Run();
"""

with open("QuotesApp.ApiService/Program.cs", "w", encoding="utf-8") as f:
    f.write(api_program)

# appsettings.json
appsettings = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"""

with open("QuotesApp.ApiService/appsettings.json", "w", encoding="utf-8") as f:
    f.write(appsettings)

print("✅ Создан файл: QuotesApp.ApiService/Program.cs")
print("✅ Создан файл: QuotesApp.ApiService/appsettings.json")

 # Result 
✅ Создан файл: QuotesApp.ApiService/Program.cs
✅ Создан файл: QuotesApp.ApiService/appsettings.json
-----------------------------------------------
# QuotesApp.Web/QuotesApp.Web.csproj
web_csproj = """<Project Sdk="Microsoft.NET.Sdk.Web">
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
"""

with open("QuotesApp.Web/QuotesApp.Web.csproj", "w", encoding="utf-8") as f:
    f.write(web_csproj)

print("✅ Создан файл: QuotesApp.Web/QuotesApp.Web.csproj")

 # Result 
✅ Создан файл: QuotesApp.Web/QuotesApp.Web.csproj
------------------
# QuotesApp.Web/Program.cs
web_program = """using QuotesApp.Web;
using QuotesApp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<QuotesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapDefaultEndpoints();
app.Run();
"""

with open("QuotesApp.Web/Program.cs", "w", encoding="utf-8") as f:
    f.write(web_program)

# appsettings.json
appsettings = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"""

with open("QuotesApp.Web/appsettings.json", "w", encoding="utf-8") as f:
    f.write(appsettings)

print("✅ Создан файл: QuotesApp.Web/Program.cs")
print("✅ Создан файл: QuotesApp.Web/appsettings.json")

 # Result 
✅ Создан файл: QuotesApp.Web/Program.cs
✅ Создан файл: QuotesApp.Web/appsettings.json
------------------
# QuotesApp.Web/QuotesApiClient.cs
quotes_api_client_cs = """using System.Net.Http.Json;
using QuotesApp.Shared.Models;

namespace QuotesApp.Web;

public class QuotesApiClient(HttpClient httpClient)
{
    public async Task<List<StockQuote>> GetQuotesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var quotes = await httpClient.GetFromJsonAsync<List<StockQuote>>("/quotes", cancellationToken);
            return quotes ?? new List<StockQuote>();
        }
        catch
        {
            return new List<StockQuote>();
        }
    }

    public async Task<StockQuote?> GetQuotesByPortfolioAsync(string portfolio, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<StockQuote>($"/quotes/{portfolio}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
"""

with open("QuotesApp.Web/QuotesApiClient.cs", "w", encoding="utf-8") as f:
    f.write(quotes_api_client_cs)

print("✅ Создан файл: QuotesApp.Web/QuotesApiClient.cs")

 # Result 
✅ Создан файл: QuotesApp.Web/QuotesApiClient.cs
---------------
# Создаем структуру папок для компонентов
os.makedirs("QuotesApp.Web/Components", exist_ok=True)

# App.razor
app_razor = """<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
"""

with open("QuotesApp.Web/Components/App.razor", "w", encoding="utf-8") as f:
    f.write(app_razor)

# Routes.razor
routes_razor = """<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
"""

with open("QuotesApp.Web/Components/Routes.razor", "w", encoding="utf-8") as f:
    f.write(routes_razor)

print("✅ Создан файл: QuotesApp.Web/Components/App.razor")
print("✅ Создан файл: QuotesApp.Web/Components/Routes.razor")

 # Result 
✅ Создан файл: QuotesApp.Web/Components/App.razor
✅ Создан файл: QuotesApp.Web/Components/Routes.razor
--------------------------------------
# Создаем папку Layout
os.makedirs("QuotesApp.Web/Components/Layout", exist_ok=True)

# MainLayout.razor
main_layout_razor = """@inherits LayoutComponentBase

<div class="page">
    <main>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="" class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>
"""

with open("QuotesApp.Web/Components/Layout/MainLayout.razor", "w", encoding="utf-8") as f:
    f.write(main_layout_razor)

print("✅ Создан файл: QuotesApp.Web/Components/Layout/MainLayout.razor")

 # Result 
✅ Создан файл: QuotesApp.Web/Components/Layout/MainLayout.razor
--------------------------------
# Создаем папку Pages
os.makedirs("QuotesApp.Web/Components/Pages", exist_ok=True)

# Home.razor - главная страница с котировками
home_razor = """@page "/"
@using QuotesApp.Shared.Models
@using QuotesApp.Web
@inject QuotesApiClient QuotesApiClient
@inject StackExchange.Redis.IConnectionMultiplexer Redis
@rendermode InteractiveServer
@implements IAsyncDisposable

<PageTitle>Котировки акций — QuotesApp</PageTitle>

<div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h1 class="mb-0">
            📈 Котировки акций
            @if (_autoRefresh)
            {
                <span class="badge bg-success ms-2 fs-6" style="animation: pulse 1.5s infinite">
                    <span class="me-1">●</span>LIVE
                </span>
            }
        </h1>
    </div>
    <p class="text-muted">
        Данные получены из <code>ApiService</code> через Aspire Service Discovery.
        Real-time обновления через Redis Pub/Sub.
    </p>

    <!-- Панель управления автообновлением -->
    <div class="card mb-4">
        <div class="card-body d-flex align-items-center gap-3 flex-wrap">
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="autoRefreshSwitch"
                       @bind="_autoRefresh" @bind:after="OnAutoRefreshChanged">
                <label class="form-check-label" for="autoRefreshSwitch">
                    Автообновление
                </label>
            </div>
            <div class="d-flex align-items-center gap-2">
                <label for="intervalSelect" class="form-label mb-0">Период:</label>
                <select id="intervalSelect" class="form-select form-select-sm" style="width: auto"
                        @bind="_intervalSeconds" @bind:after="OnIntervalChanged">
                    <option value="3">3 секунды</option>
                    <option value="5">5 секунд</option>
                    <option value="10">10 секунд</option>
                    <option value="30">30 секунд</option>
                    <option value="60">1 минута</option>
                </select>
            </div>
            @if (_lastUpdate != default)
            {
                <small class="text-muted">
                    🕐 Обновлено: @_lastUpdate.ToString("HH:mm:ss")
                </small>
            }
            <button class="btn btn-sm btn-outline-primary ms-auto" @onclick="ManualRefreshAsync">
                🔄 Обновить сейчас
            </button>
        </div>
    </div>

    <!-- Фильтр по портфелям -->
    <div class="card mb-4">
        <div class="card-body">
            <div class="d-flex gap-2 flex-wrap">
                <button class="btn @(string.IsNullOrEmpty(_selectedPortfolio) ? "btn-primary" : "btn-outline-primary")"
                        @onclick='() => SelectPortfolio("")'>
                    Все портфели
                </button>
                <button class="btn @(_selectedPortfolio == "tech" ? "btn-success" : "btn-outline-success")"
                        @onclick='() => SelectPortfolio("tech")'>
                    🖥️ Tech
                </button>
                <button class="btn @(_selectedPortfolio == "consumer" ? "btn-warning" : "btn-outline-warning")"
                        @onclick='() => SelectPortfolio("consumer")'>
                    🛒 Consumer
                </button>
                <button class="btn @(_selectedPortfolio == "finance" ? "btn-info" : "btn-outline-info")"
                        @onclick='() => SelectPortfolio("finance")'>
                    💰 Finance
                </button>
                <button class="btn @(_selectedPortfolio == "energy" ? "btn-danger" : "btn-outline-danger")"
                        @onclick='() => SelectPortfolio("energy")'>
                    ⚡ Energy
                </button>
            </div>
        </div>
    </div>

    <!-- Состояния -->
    @if (_isLoading && _quotes is null)
    {
        <div class="alert alert-info">
            <span class="spinner-border spinner-border-sm me-2"></span>
            Первичная загрузка данных из ApiService...
        </div>
    }
    else if (_error is not null)
    {
        <div class="alert alert-danger">
            <strong>Ошибка:</strong> @_error
            @if (_autoRefresh)
            {
                <small class="d-block mt-1">
                    Автообновление продолжит попытки через @_intervalSeconds сек.
                </small>
            }
        </div>
    }

    <!-- Таблица котировок -->
    @if (_quotes is { Count: > 0 })
    {
        <div class="position-relative">
            @if (_isRefreshing)
            {
                <div class="position-absolute top-0 end-0 m-2">
                    <span class="spinner-border spinner-border-sm text-primary" role="status"></span>
                </div>
            }
            <table class="table table-striped table-hover">
                <thead class="table-dark">
                    <tr>
                        <th>Портфель</th>
                        <th>Тикер</th>
                        <th>Bid</th>
                        <th>Ask</th>
                        <th>Last</th>
                        <th>Volume</th>
                        <th>Время</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var quote in GetFilteredQuotes())
                    {
                        <tr>
                            <td>
                                <span class="badge @GetPortfolioBadgeColor(quote.Portfolio)">
                                    @quote.Portfolio
                                </span>
                            </td>
                            <td><strong>@quote.Ticker</strong></td>
                            <td>@quote.Bid.ToString("C")</td>
                            <td>@quote.Ask.ToString("C")</td>
                            <td class="fw-bold">@quote.Last.ToString("C")</td>
                            <td>@quote.Volume.ToString("N0")</td>
                            <td>@quote.Timestamp.ToLocalTime().ToString("HH:mm:ss")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
    else if (!_isLoading)
    {
        <div class="alert alert-warning">Данные о котировках не получены.</div>
    }
</div>

<style>
    @@keyframes pulse {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.6; }
    }
</style>

@code {
    private List<StockQuote>? _quotes;
    private bool _isLoading = true;
    private bool _isRefreshing;
    private string? _error;
    private DateTime _lastUpdate;
    private string? _selectedPortfolio;
    
    // Настройки автообновления
    private bool _autoRefresh = true;
    private int _intervalSeconds = 5;
    
    // Управление жизненным циклом таймера
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    
    // Redis подписка
    private StackExchange.Redis.ChannelMessageQueue? _subscription;

    protected override async Task OnInitializedAsync()
    {
        // Первичная загрузка
        await LoadQuotesAsync(isInitial: true);
        
        // Подписываемся на Redis Pub/Sub для real-time обновлений
        SubscribeToRedis();
        
        // Запускаем цикл автообновления
        StartAutoRefresh();
    }

    private void SubscribeToRedis()
    {
        try
        {
            var subscriber = Redis.GetSubscriber();
            _subscription = subscriber.Subscribe(StackExchange.Redis.RedisChannel.Literal("quotes:all"));
            _subscription.OnMessage(async channelMessage =>
            {
                // При получении нового сообщения обновляем данные
                await InvokeAsync(async () =>
                {
                    await LoadQuotesAsync(isInitial: false);
                    StateHasChanged();
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подписки на Redis: {ex.Message}");
        }
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    if (!_autoRefresh) continue;
                    await LoadQuotesAsync(isInitial: false);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoRefresh] Ошибка: {ex.Message}");
            }
        }, token);
    }

    private void StopAutoRefresh()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _cts?.Dispose();
        _timer = null;
        _cts = null;
    }

    private async Task LoadQuotesAsync(bool isInitial)
    {
        if (isInitial)
        {
            _isLoading = true;
        }
        else
        {
            _isRefreshing = true;
        }
        _error = null;

        try
        {
            _quotes = await QuotesApiClient.GetQuotesAsync();
            _lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isLoading = false;
            _isRefreshing = false;
        }
    }

    private async Task ManualRefreshAsync()
    {
        await LoadQuotesAsync(isInitial: false);
    }

    private void OnAutoRefreshChanged()
    {
        if (_autoRefresh && _timer is null)
        {
            StartAutoRefresh();
        }
        else if (!_autoRefresh)
        {
            StopAutoRefresh();
        }
    }

    private void OnIntervalChanged()
    {
        if (_autoRefresh)
        {
            StartAutoRefresh();
        }
    }

    private void SelectPortfolio(string portfolio)
    {
        _selectedPortfolio = string.IsNullOrEmpty(portfolio) ? null : portfolio;
    }

    private IEnumerable<StockQuote> GetFilteredQuotes()
    {
        if (_quotes is null) return Enumerable.Empty<StockQuote>();
        
        if (string.IsNullOrEmpty(_selectedPortfolio))
        {
            return _quotes;
        }
        
        return _quotes.Where(q => q.Portfolio.Equals(_selectedPortfolio, StringComparison.OrdinalIgnoreCase));
    }

    private string GetPortfolioBadgeColor(string portfolio) => portfolio.ToLower() switch
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
        
        if (_subscription != null)
        {
            try
            {
                var subscriber = Redis.GetSubscriber();
                await subscriber.UnsubscribeAsync(StackExchange.Redis.RedisChannel.Literal("quotes:all"));
            }
            catch { }
        }
    }
}
"""

with open("QuotesApp.Web/Components/Pages/Home.razor", "w", encoding="utf-8") as f:
    f.write(home_razor)

print("✅ Создан файл: QuotesApp.Web/Components/Pages/Home.razor")

 # Result 
✅ Создан файл: QuotesApp.Web/Components/Pages/Home.razor
----------------------------------
# QuotesApp.AppHost/QuotesApp.AppHost.csproj
apphost_csproj = """<Project Sdk="Aspire.AppHost.Sdk/13.4.6">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\QuotesApp.PipeServer\\QuotesApp.PipeServer.csproj" />
    <ProjectReference Include="..\\QuotesApp.ApiService\\QuotesApp.ApiService.csproj" />
    <ProjectReference Include="..\\QuotesApp.Web\\QuotesApp.Web.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Redis" Version="13.4.6" />
  </ItemGroup>
</Project>
"""

with open("QuotesApp.AppHost/QuotesApp.AppHost.csproj", "w", encoding="utf-8") as f:
    f.write(apphost_csproj)

print("✅ Создан файл: QuotesApp.AppHost/QuotesApp.AppHost.csproj")

 # Result 
✅ Создан файл: QuotesApp.AppHost/QuotesApp.AppHost.csproj

------------------
# QuotesApp.AppHost/Program.cs
apphost_program = """var builder = DistributedApplication.CreateBuilder(args);

// Добавляем Redis для кэширования и Pub/Sub
var cache = builder.AddRedis("cache");

// Добавляем PipeServer - генератор котировок
var pipeServer = builder.AddProject<Projects.QuotesApp_PipeServer>("pipeserver")
    .WithReference(cache)
    .WaitFor(cache);

// Добавляем ApiService - REST API для получения котировок
var apiService = builder.AddProject<Projects.QuotesApp_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health");

// Добавляем Web - Blazor Server UI
builder.AddProject<Projects.QuotesApp_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
"""

with open("QuotesApp.AppHost/Program.cs", "w", encoding="utf-8") as f:
    f.write(apphost_program)

# appsettings.json
appsettings = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  }
}
"""

with open("QuotesApp.AppHost/appsettings.json", "w", encoding="utf-8") as f:
    f.write(appsettings)

# appsettings.Development.json
appsettings_dev = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
"""

with open("QuotesApp.AppHost/appsettings.Development.json", "w", encoding="utf-8") as f:
    f.write(appsettings_dev)

print("✅ Создан файл: QuotesApp.AppHost/Program.cs")
print("✅ Создан файл: QuotesApp.AppHost/appsettings.json")
print("✅ Создан файл: QuotesApp.AppHost/appsettings.Development.json")

 # Result 
✅ Создан файл: QuotesApp.AppHost/Program.cs
✅ Создан файл: QuotesApp.AppHost/appsettings.json
✅ Создан файл: QuotesApp.AppHost/appsettings.Development.json
-------------------------
# Создаем папку Properties
os.makedirs("QuotesApp.AppHost/Properties", exist_ok=True)

# launchSettings.json
launch_settings = """{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17008;http://localhost:15141",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21133",
        "ASPIRE_DASHBOARD_MCP_ENDPOINT_URL": "https://localhost:23063",
        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22139"
      }
    },
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:15141",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19100",
        "ASPIRE_DASHBOARD_MCP_ENDPOINT_URL": "http://localhost:18037",
        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "http://localhost:20133"
      }
    }
  }
}
"""

with open("QuotesApp.AppHost/Properties/launchSettings.json", "w", encoding="utf-8") as f:
    f.write(launch_settings)

print("✅ Создан файл: QuotesApp.AppHost/Properties/launchSettings.json")

 # Result 
✅ Создан файл: QuotesApp.AppHost/Properties/launchSettings.json
--------------------------
# QuotesApp.sln
solution = """
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.Shared", "QuotesApp.Shared\QuotesApp.Shared.csproj", "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.ServiceDefaults", "QuotesApp.ServiceDefaults\QuotesApp.ServiceDefaults.csproj", "{B2C3D4E5-F6A7-8901-BCDE-F12345678901}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.PipeServer", "QuotesApp.PipeServer\QuotesApp.PipeServer.csproj", "{C3D4E5F6-A7B8-9012-CDEF-123456789012}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.ApiService", "QuotesApp.ApiService\QuotesApp.ApiService.csproj", "{D4E5F6A7-B8C9-0123-DEF1-234567890123}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.Web", "QuotesApp.Web\QuotesApp.Web.csproj", "{E5F6A7B8-C9D0-1234-EF12-345678901234}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "QuotesApp.AppHost", "QuotesApp.AppHost\QuotesApp.AppHost.csproj", "{F6A7B8C9-D0E1-2345-F123-456789012345}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.Build.0 = Release|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Release|Any CPU.Build.0 = Release|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Release|Any CPU.Build.0 = Release|Any CPU
		{D4E5F6A7-B8C9-0123-DEF1-234567890123}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D4E5F6A7-B8C9-0123-DEF1-234567890123}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D4E5F6A7-B8C9-0123-DEF1-234567890123}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D4E5F6A7-B8C9-0123-DEF1-234567890123}.Release|Any CPU.Build.0 = Release|Any CPU
		{E5F6A7B8-C9D0-1234-EF12-345678901234}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E5F6A7B8-C9D0-1234-EF12-345678901234}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E5F6A7B8-C9D0-1234-EF12-345678901234}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{E5F6A7B8-C9D0-1234-EF12-345678901234}.Release|Any CPU.Build.0 = Release|Any CPU
		{F6A7B8C9-D0E1-2345-F123-456789012345}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F6A7B8C9-D0E1-2345-F123-456789012345}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F6A7B8C9-D0E1-2345-F123-456789012345}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F6A7B8C9-D0E1-2345-F123-456789012345}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
"""

with open("QuotesApp.sln", "w", encoding="utf-8") as f:
    f.write(solution)

print("✅ Создан файл: QuotesApp.sln")

 # Result 
✅ Создан файл: QuotesApp.sln
-------------------
PipeServer (генерация) 
    ↓
Redis Pub/Sub (трансляция)
    ↓
ApiService (REST API)
    ↓
Web UI (Blazor Server)
--------------------------------------
# Перейдите в папку проекта
cd QuotesApp

# Запустите через AppHost (рекомендуется)
dotnet run --project QuotesApp.AppHost
-----------------------------------
    "# Перейдите в папку проекта\n",
    "cd QuotesApp\n",
    "\n",
    "# Запустите через AppHost (рекомендуется)\n",
    "dotnet run --project QuotesApp.AppHost\n",
    "```
------------------
dotnet run --project QuotesApp.AppHost
----------------
readme_text += "dotnet run --project QuotesApp.AppHost\n"
readme_text += "```
----------------------
dotnet run --project QuotesApp.AppHost
