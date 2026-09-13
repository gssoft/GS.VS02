using TradeSystem.Contracts;
using TradeSystem.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
//----------------
// --- Mock data ---
var portfolios = new List<Portfolio>
{
    new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Default Portfolio", IsActive = true }
};

var tickers = new List<Ticker>
{
    new() { Id = Guid.NewGuid(), Symbol = "SBER", Board = "TQBR", LotSize = 10 },
    new() { Id = Guid.NewGuid(), Symbol = "GAZP", Board = "TQBR", LotSize = 10 },
    new() { Id = Guid.NewGuid(), Symbol = "SRZ5", Board = "RFUD", LotSize = 1 }
};

var strategies = new List<TradeStrategy>
{
    new() { Id = Guid.NewGuid(), Name = "MeanReversion", StrategyType = "MeanReversion" },
    new() { Id = Guid.NewGuid(), Name = "Momentum", StrategyType = "Momentum" }
};

var tickerSber = tickers[0];
var strategyMr = strategies[0];

var bindings = new List<TickerStrategyBinding>
{
    new()
    {
        Id = Guid.NewGuid(),
        PortfolioId = portfolios[0].Id,
        TickerId = tickerSber.Id,
        StrategyId = strategyMr.Id,
        IsActive = true,
        CapitalAllocation = 100000,
        MaxPosition = 1000,
        NetPosition = 500,
        AvgEntryPrice = 250.50m,
        RealizedPnL = 1200.00m,
        UnrealizedPnL = 350.00m,
        Ticker = tickerSber,
        Strategy = strategyMr,
        Portfolio = portfolios[0]
    }
};

// --- Endpoints ---

app.MapGet("/api/portfolios", () => portfolios);
app.MapGet("/api/portfolios/{id:guid}", (Guid id) =>
    portfolios.FirstOrDefault(p => p.Id == id));

app.MapGet("/api/tickers", () => tickers);
app.MapGet("/api/strategies", () => strategies);

app.MapGet("/api/bindings", (Guid? portfolioId) =>
{
    if (portfolioId.HasValue)
        return bindings.Where(b => b.PortfolioId == portfolioId.Value).ToList();
    return bindings;
});

app.MapGet("/api/portfolios/{id:guid}/snapshot", (Guid id) =>
{
    var posSnapshots = bindings
        .Where(b => b.PortfolioId == id && b.IsActive)
        .Select(b => new PositionSnapshot(
            b.Id,
            b.Ticker?.Symbol ?? "",
            b.Strategy?.Name ?? "",
            b.NetPosition,
            b.AvgEntryPrice,
            b.RealizedPnL,
            b.UnrealizedPnL
        )).ToList();

    return new PortfolioSnapshot(
        id,
        portfolios.FirstOrDefault(p => p.Id == id)?.Name ?? "",
        posSnapshots.Sum(p => p.RealizedPnL),
        posSnapshots.Sum(p => p.UnrealizedPnL),
        posSnapshots
    );
});

app.MapGet("/api/trades", (Guid? portfolioId) =>
{
    return new List<TradeDto>
    {
        new(Guid.NewGuid(), Guid.NewGuid(), "SBER", "Buy", 100, 250.10m, DateTime.UtcNow.AddMinutes(-30)),
        new(Guid.NewGuid(), Guid.NewGuid(), "SBER", "Buy", 400, 250.40m, DateTime.UtcNow.AddMinutes(-20)),
        new(Guid.NewGuid(), Guid.NewGuid(), "SBER", "Sell", 100, 251.20m, DateTime.UtcNow.AddMinutes(-5))
    };
});


// -------------
// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
