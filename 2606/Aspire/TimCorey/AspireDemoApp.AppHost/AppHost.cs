var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// RabbitMQ в контейнере (по умолчанию Aspire использует образ и поднимает через Docker)
var rabbit = builder.AddRabbitMQ("rabbit")
    .WithManagementPlugin(); // чтобы была web-админка (15672)

// ваш API
var apiService = builder.AddProject<Projects.AspireDemoApp_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(rabbit)
    .WaitFor(cache)
    .WaitFor(rabbit)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireDemoApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

//var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");

//var apiService = builder.AddProject<Projects.AspireDemoApp_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.AspireDemoApp_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(cache)
//    .WaitFor(cache)
//    .WithReference(apiService)
//    .WaitFor(apiService);

//builder.Build().Run();
