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

app.MapGet("/quotes/{portfolio}", async (string portfolio, IConnectionMultiplexer redis) =>
{
    if (!PortfolioDefinition.Portfolios.ContainsKey(portfolio))
        return Results.NotFound($"Portfolio '{portfolio}' not found.");

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
