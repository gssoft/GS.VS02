var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
// builder.Services.AddDaprClient();  // 👈 ВРЕМЕННО УБРАЛИ

var app = builder.Build();

app.UseExceptionHandler();
// app.UseCloudEvents();  // 👈 ВРЕМЕННО УБРАЛИ

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running");

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

// app.MapSubscribeHandler();  // 👈 ВРЕМЕННО УБРАЛИ

app.MapDefaultEndpoints();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();
//builder.Services.AddProblemDetails();
//builder.Services.AddOpenApi();

//// Добавляем Dapr SDK
//builder.Services.AddDaprClient();

//var app = builder.Build();

//app.UseExceptionHandler();
//app.UseCloudEvents(); // 👈 ВАЖНО: middleware для CloudEvents

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

//app.MapGet("/", () => "API service is running");

//app.MapGet("/weatherforecast", async (Dapr.Client.DaprClient dapr, ILogger<Program> logger) =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//    .ToArray();

//    // 🔥 Публикуем событие в Pub/Sub
//    var forecastEvent = new ForecastGeneratedEvent
//    {
//        GeneratedAt = DateTime.UtcNow,
//        ForecastCount = forecast.Length,
//        AverageTempC = (int)forecast.Average(f => f.TemperatureC)
//    };

//    await dapr.PublishEventAsync(
//        pubsubName: "pubsub",           // имя Dapr компонента
//        topicName: "forecast-generated", // тема
//        data: forecastEvent);            // данные события

//    logger.LogInformation("📢 Published event: {Count} forecasts, avg temp {Avg}°C",
//        forecast.Length, forecastEvent.AverageTempC);

//    return forecast;
//})
//.WithName("GetWeatherForecast");

//// Подписка на собственные события (для логирования)
//app.MapSubscribeHandler();

//app.MapDefaultEndpoints();
//app.Run();

//record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}

//public class ForecastGeneratedEvent
//{
//    public DateTime GeneratedAt { get; set; }
//    public int ForecastCount { get; set; }
//    public int AverageTempC { get; set; }
//}

// -------------------------------------------------------------

//var builder = WebApplication.CreateBuilder(args);

//// Add service defaults & Aspire client integrations.
//builder.AddServiceDefaults();

//// Add services to the container.
//builder.Services.AddProblemDetails();

//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//app.UseExceptionHandler();

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

//app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

//app.MapDefaultEndpoints();

//app.Run();

//record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
