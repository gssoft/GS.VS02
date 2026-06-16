using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using NamedPipes.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов
builder.Services.AddHostedService<QuoteServerService>();
builder.Services.AddHostedService<QuoteClientService>();

var app = builder.Build();

// Маршрутизация страницы
app.MapGet("/", () => "IPC Services are running in background. Watch the console.");

// Запуск приложения
app.Run();
