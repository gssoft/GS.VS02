//var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");

//var apiService = builder.AddProject<Projects.Trading_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.Trading_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(cache)
//    .WaitFor(cache)
//    .WithReference(apiService)
//    .WaitFor(apiService);

//builder.AddProject<Projects.Trading_Api_Orders>("trading-api-orders");

//builder.AddProject<Projects.Trading_Api_MarketData>("trading-api-marketdat");

//builder.Build().Run();
