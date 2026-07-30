var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithEndpoint(port: 6380, targetPort: 6379);  // избегаем конфликта с dapr_redis

var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");
// .WithDaprSidecar("apiservice")  // 👈 ВРЕМЕННО УБРАЛИ

var web = builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    // .WithDaprSidecar("webfrontend")  // 👈 ВРЕМЕННО УБРАЛИ
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

//using CommunityToolkit.Aspire.Hosting.Dapr;

//var builder = DistributedApplication.CreateBuilder(args);

//// Инициализируем Dapr
//builder.AddDapr();

//// ✅ Redis для Output Cache на ДРУГОМ порту (не 6379!)
//// dapr_redis уже занял 6379, поэтому используем 6380
//var cache = builder.AddRedis("cache")
//    .WithEndpoint(port: 6380, targetPort: 6379);

//var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health")
//    .WithDaprSidecar("apiservice");

//var web = builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithDaprSidecar("webfrontend")
//    .WithReference(cache)
//    .WaitFor(cache)
//    .WithReference(apiService)
//    .WaitFor(apiService);

//builder.Build().Run();



