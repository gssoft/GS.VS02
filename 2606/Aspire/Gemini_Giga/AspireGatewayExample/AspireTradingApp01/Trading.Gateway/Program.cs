var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); // OpenTelemetry, service discovery, resilience

// Настраиваем YARP как обратный прокси
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints(); // /health, /alive
app.MapReverseProxy();

app.Run();
