//// File: Services/QuotesGeneratorService.cs
//using Quotes.Shared.Contracts;
//using System.Threading.Channels;

//namespace AspireApp1.ApiService.Services;

//public class QuotesGeneratorService : BackgroundService
//{
//    private readonly ILogger<QuotesGeneratorService> _logger;
//    private readonly ChannelWriter<QuoteDto> _writer;
//    private static readonly string[] FinanceTickers = { "SBER", "GAZP", "LKOH", "YNDX", "TATN" };
//    private static readonly string[] Summaries = { "Up", "Down", "Flat", "Volatile" };
//    private readonly Random _rand = new();

//    public QuotesGeneratorService(ILogger<QuotesGeneratorService> logger, ChannelWriter<QuoteDto> writer)
//    {
//        _logger = logger;
//        _writer = writer;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var ticker = FinanceTickers[_rand.Next(FinanceTickers.Length)];
//            var basePrice = _rand.Next(150, 450) + _rand.NextDecimal(); // Цена от 150 до 450 руб.

//            var quote = new QuoteDto(
//                Ticker: ticker,
//                Last: Math.Round(basePrice, 2),
//                Volume: _rand.Next(10, 5000),
//                TimestampUtc: DateTimeOffset.UtcNow);

//            // Логируем каждый тик (ваша задача №1)
//            _logger.LogInformation("Generated tick: {@Quote}", quote);

//            await _writer.WriteAsync(quote, stoppingToken);

//            // Частота обновления котировок
//            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
//        }
//    }
//}

namespace AspireApp1.ApiService.Services;

// Extension for Random to get decimals
public static class RandomExtensions
{
    public static decimal NextDecimal(this Random random)
    {
        return (decimal)random.NextDouble();
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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
