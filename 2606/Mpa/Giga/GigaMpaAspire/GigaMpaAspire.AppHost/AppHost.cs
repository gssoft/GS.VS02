// src/MicroPlatform.AppHost/Program.cs

using Aspire.Hosting;
using MicroPlatform.Abstractions;
using MicroPlatform.Core; // Для InMemory шины (можно заменить на Infrastructure)
using MicroPlatform.Infrastructure; // Для RabbitMQ шины
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Включает телеметрию, health checks

// --- Регистрация RabbitMQ (Брокер) ---
var messaging = builder.AddRabbitMQ("messaging");
messaging.WithVolume("rabbit-data", "/var/lib/rabbitmq");
messaging.WithAnnotation("replicas", 1); // Можно увеличить

// --- Регистрация IMicroEventBus ---
builder.Services.AddSingleton<IMicroEventBus>(sp =>
{
    // В продакшене используем RabbitMQ:
    var connection = sp.GetRequiredService<IConnection>();
    var logger = sp.GetRequiredService<ILogger<RabbitMqMicroEventBus>>();

    return new RabbitMqMicroEventBus(
        connectionFactory: new ConnectionFactory() { /* Настройки можно брать из конфига */ },
        logger,
        connection: connection // Передаем существующее соединение от Aspire!
    );
});
// Для тестов можно использовать:
// builder.Services.AddSingleton<IMicroEventBus, InMemoryMicroEventBus>();


// --- Регистрация Процессоров ---
builder.Services.AddOrderProcessor();
builder.Services.AddInventoryProcessor();
builder.Services.AddInvoiceProcessor();


var app = builder.Build();
app.MapDefaultEndpoints(); // Маппинг /healthz, /metrics и т.д.
app.Run();
