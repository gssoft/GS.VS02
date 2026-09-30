var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

// Имитация рыночных данных
var marketData = new Dictionary<string, decimal>
{
    ["AAPL"] = 175.50m,
    ["MSFT"] = 420.30m,
    ["GOOGL"] = 155.80m
};

app.MapGet("/api/marketdata/{symbol}", (string symbol) =>
{
    if (marketData.TryGetValue(symbol.ToUpper(), out var price))
        return Results.Ok(new { Symbol = symbol.ToUpper(), Price = price, Timestamp = DateTime.UtcNow });

    return Results.NotFound($"Symbol {symbol} not found");
});

app.MapGet("/api/marketdata", () => Results.Ok(marketData));

app.Run();

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
