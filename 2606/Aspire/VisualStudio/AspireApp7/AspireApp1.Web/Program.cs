using AspireApp1.Web;
using AspireApp1.Web.Components;
using AspireApp1.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// builder.Services.AddDaprClient();  // 👈 ВРЕМЕННО УБРАЛИ
builder.Services.AddSingleton<NotificationHub>();  // 👈 ВРЕМЕННО УБРАЛИ

builder.Services.AddHttpClient<WeatherApiClient>(client =>
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
app.UseOutputCache();  // 👈 РАСКОММЕНТИРОВАЛИ (должно быть!)

// app.UseCloudEvents();  // 👈 ВРЕМЕННО УБРАЛИ

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// УБРАЛИ эндпоинт подписки на события временно

// app.MapSubscribeHandler();  // 👈 ВРЕМЕННО УБРАЛИ

app.MapDefaultEndpoints();
app.Run();

// 2 using AspireApp1.Web;
//using AspireApp1.Web.Components;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();
//builder.AddRedisOutputCache("cache");  // ← это подключится к порту 6380 автоматически

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//{
//    client.BaseAddress = new("https+http://apiservice");
//});

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseAntiforgery();
//app.UseOutputCache();

//app.MapStaticAssets();
//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.MapDefaultEndpoints();
//app.Run();

// 1 -----------------------
//using AspireApp1.Web;
//using AspireApp1.Web.Components;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();
//builder.AddRedisOutputCache("cache");

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//// Dapr SDK для подписки
//builder.Services.AddDaprClient();

//// Хранилище уведомлений (in-memory для демо)
//builder.Services.AddSingleton<NotificationHub>();

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//{
//    client.BaseAddress = new("https+http://apiservice");
//});

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseAntiforgery();
//app.UseOutputCache();
//app.UseCloudEvents(); // 👈 ВАЖНО для Pub/Sub

//app.MapStaticAssets();
//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//// 🔥 Эндпоинт для подписки на события
//app.MapPost("/forecast-events", async (
//    ForecastGeneratedEvent evt,
//    NotificationHub hub,
//    ILogger<Program> logger) =>
//{
//    logger.LogInformation("📥 Received event: {Count} forecasts at {Time}",
//        evt.ForecastCount, evt.GeneratedAt);

//    hub.AddNotification($"📢 Новый прогноз: {evt.ForecastCount} дней, средняя {evt.AverageTempC}°C");

//    return Results.Ok();
//})
//.WithTopic("pubsub", "forecast-generated"); // 👈 Подписка на тему

//app.MapSubscribeHandler();
//app.MapDefaultEndpoints();
//app.Run();

//public class ForecastGeneratedEvent
//{
//    public DateTime GeneratedAt { get; set; }
//    public int ForecastCount { get; set; }
//    public int AverageTempC { get; set; }
//}

//public class NotificationHub
//{
//    private readonly List<string> _notifications = new();
//    private readonly object _lock = new();

//    public void AddNotification(string message)
//    {
//        lock (_lock)
//        {
//            _notifications.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
//            if (_notifications.Count > 20)
//                _notifications.RemoveAt(_notifications.Count - 1);
//        }
//        OnNotificationReceived?.Invoke(message);
//    }

//    public IReadOnlyList<string> GetNotifications()
//    {
//        lock (_lock) return _notifications.ToList();
//    }

//    public event Action<string>? OnNotificationReceived;
//}
//// ------------------------------------------------------------------------
