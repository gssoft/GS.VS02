var builder = DistributedApplication.CreateBuilder(args);

// 1. Бизнес-сервисы (не знают друг о друге)
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");

// 2. Центральный шлюз (роутер). Ссылается на бизнес-сервисы.
//    Никто, кроме Gateway, не видит Orders и MarketData.
var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(marketDataApi);

// 3. Фронтенд. Ссылается ТОЛЬКО на Gateway.
//    Он физически не может обратиться к Orders или MarketData напрямую.
var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(gateway);

builder.Build().Run();

//var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");

//var apiService = builder.AddProject<Projects.AspireTradingApp01_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.AspireTradingApp01_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(cache)
//    .WaitFor(cache)
//    .WithReference(apiService)
//    .WaitFor(apiService);

//builder.Build().Run();
