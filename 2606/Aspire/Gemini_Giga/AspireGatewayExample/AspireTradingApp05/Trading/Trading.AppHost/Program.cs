var builder = DistributedApplication.CreateBuilder(args);


// Настраиваем MassTransit для подключения к RabbitMQ.
// "rabbit" — это имя ресурса, которое мы задали в AppHost.
builder.AddMassTransitRabbitMq("rabbit", x =>
{
    // Здесь можно зарегистрировать потребителей (consumers), но Orders API их не имеет.
    // x.AddConsumer<SomeConsumer>(); 
});


// ─── Инфраструктура обмена сообщениями ───
// Aspire развернет RabbitMQ в Docker и предоставит строку подключения.
var messaging = builder.AddRabbitMQ("rabbit")
    .WithManagementPlugin(); // Включаем веб-интерфейс для отладки


// 1. Бизнес-сервисы (не знают друг о друге)
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");



// 2. Центральный шлюз (роутер). Ссылается на бизнес-сервисы.
//    Никто, кроме Gateway, не видит Orders и MarketData.

var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithReference(ordersApi)
    .WithReference(marketDataApi);

//var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
//    .WithHttpEndpoint(port: 5000, name: "http")   // ← добавить эту строку
//    .WithReference(ordersApi)
//    .WithReference(marketDataApi);



//var gateway = builder.AddProject<Projects.Trading_Gateway>("gateway")
//    .WithReference(ordersApi)
//    .WithReference(marketDataApi);

// 3. Фронтенд. Ссылается ТОЛЬКО на Gateway.
//    Он физически не может обратиться к Orders или MarketData напрямую.
var web = builder.AddProject<Projects.Trading_Web>("web")
    .WithReference(gateway);

builder.AddProject<Projects.Trading_Api_Watchlist>("trading-api-watchlist");

builder.AddProject<Projects.Trading_Api_TradeFeed>("trading-api-tradefeed");

builder.Build().Run();
