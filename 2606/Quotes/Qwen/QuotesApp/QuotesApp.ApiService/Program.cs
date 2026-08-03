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
            var json = entry.Value.ToString();
            if (string.IsNullOrWhiteSpace(json)) continue;

            var q = JsonSerializer.Deserialize<StockQuote>(json);
            if (q is not null) quotes.Add(q);
        }
    }

    return Results.Ok(quotes);
})
.WithName("GetAllQuotes"); // <-- .WithOpenApi() просто убрано

app.MapGet("/quotes/{portfolio}", async (string portfolio, IConnectionMultiplexer redis) =>
{
    if (!PortfolioDefinition.Portfolios.ContainsKey(portfolio))
        return Results.NotFound($"Portfolio '{portfolio}' not found.");

    var db = redis.GetDatabase();
    var entries = await db.HashGetAllAsync($"dashboard:{portfolio}");

    var quotes = entries
        .Select(e => e.Value.ToString())
        .Where(json => !string.IsNullOrWhiteSpace(json))
        .Select(json => JsonSerializer.Deserialize<StockQuote>(json))
        .Where(q => q is not null)
        .ToList();

    return Results.Ok(quotes);
})
.WithName("GetQuotesByPortfolio"); // <-- .WithOpenApi() убрано

app.MapGet("/portfolios", () =>
{
    var list = PortfolioDefinition.Portfolios
        .Select(p => new { p.Key, p.Value.DisplayName, Tickers = p.Value.Tickers })
        .ToList();

    return Results.Ok(list);
})
.WithName("GetPortfolios"); 

app.MapDefaultEndpoints();

app.Run();

