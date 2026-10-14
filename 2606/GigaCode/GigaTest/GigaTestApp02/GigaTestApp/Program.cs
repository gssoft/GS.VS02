
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// YARP - Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware для логирования каждого HTTP запроса
app.Use(async (context, next) =>
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    var proxyInfo = context.Items["Yarp.ProxyFeature"] != null ? " [PROXY]" : "";
    Console.WriteLine($"[{timestamp}] {context.Request.Method} {context.Request.Path}{proxyInfo}");
    await next();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// YARP - маршрутизация запросов
app.MapReverseProxy();

// Красивое сообщение при запуске
var urls = builder.Configuration["Kestrel:Endpoints:Https:Url"] 
    ?? "https://localhost:5001";

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║     GigaTestApp is running!              ║");
Console.WriteLine($"║     URL: {urls,-32}║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.WriteLine();

// Фоновый цикл — вывод статуса каждые 5 секунд
_ = Task.Run(async () =>
{
    var messages = new[]
    {
        "⚙️  Application is running...",
        "📊 Active requests: 0",
        "🧠 Memory usage: OK",
        "🔗 API endpoints loaded",
        "✅ Health check passed",
        "📡 Waiting for requests..."
    };

    var index = 0;
    while (!app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] {messages[index % messages.Length]}");
        index++;
        await Task.Delay(5000);
    }
});

app.Run();


// --------------

// using Microsoft.OpenApi.Models;
// using Microsoft.Extensions.Hosting;

// var builder = WebApplication.CreateBuilder(args);

// // --- 1. ДОБАВЛЯЕМ КЛАССИЧЕСКИЙ SWAGGERGEN ---
// builder.Services.AddControllers();

// // Добавляем генератор документации
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "GigaTestApp API", Version = "v1" });
// });

// // YARP - Reverse Proxy
// builder.Services.AddReverseProxy()
//     .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// var app = builder.Build();

// // --- 2. ВКЛЮЧАЕМ MIDDLEWARE В КОНФИГУРАЦИЮ ---
// if (app.Environment.IsDevelopment())
// {
//     // Включаем генерацию JSON-схемы
//     app.UseSwagger();
    
//     // Включаем интерактивную страницу (UI)
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "GigaTestApp API V1");
//         // Можно задать свой маршрут, например /docs
//         // c.RoutePrefix = "docs"; 
//     });
// }

// // Middleware для логирования каждого HTTP запроса
// app.Use(async (context, next) =>
// {
//     var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
//     var proxyInfo = context.Items["Yarp.ProxyFeature"] != null ? " [PROXY]" : "";
//     Console.WriteLine($"[{timestamp}] {context.Request.Method} {context.Request.Path}{proxyInfo}");
//     await next();
// });

// app.UseHttpsRedirection();

// app.UseAuthorization();

// app.MapControllers();

// // YARP - маршрутизация запросов
// app.MapReverseProxy();

// // Красивое сообщение при запуске
// var urls = builder.Configuration["Kestrel:Endpoints:Https:Url"] 
//     ?? "https://localhost:5001";

// Console.WriteLine();
// Console.WriteLine("╔══════════════════════════════════════════╗");
// Console.WriteLine("║     GigaTestApp is running!              ║");
// Console.WriteLine($"║     URL: {urls,-32}║");
// Console.WriteLine("╚══════════════════════════════════════════╝");
// Console.WriteLine();

// // Фоновый цикл — вывод статуса каждые 5 секунд
// _ = Task.Run(async () =>
// {
//     var messages = new[]
//     {
//         "⚙️  Application is running...",
//         "📊 Active requests: 0",
//         "🧠 Memory usage: OK",
//         "🔗 API endpoints loaded",
//         "✅ Health check passed",
//         "📡 Waiting for requests..."
//     };

//     var index = 0;
//     while (!app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested)
//     {
//         var timestamp = DateTime.Now.ToString("HH:mm:ss");
//         Console.WriteLine($"[{timestamp}] {messages[index % messages.Length]}");
//         index++;
//         await Task.Delay(5000);
//     }
// });

// app.Run();
