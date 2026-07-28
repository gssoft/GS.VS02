// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Инфраструктура
var cache = builder.AddRedis("cache");
var rabbit = builder.AddRabbitMQ("rabbit").WithManagementPlugin();

// Сервисы
var apiService = builder.AddProject<Projects.AspireDemoApp_ApiService>("apiservice")
    .WithReference(cache) // Передаем строку подключения к Redis
    .WaitFor(cache);      // Ждем, пока Redis поднимется

builder.AddProject<Projects.AspireDemoApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService) // ВАЖНО: даем фронтенду знать об адресе API
    .WaitFor(apiService);

builder.Build().Run();
