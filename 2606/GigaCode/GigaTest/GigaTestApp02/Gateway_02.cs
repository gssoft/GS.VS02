/*
14. Как это соотносится с вашей философией «Код как Конституция»
Ваша главная идея:
Если связи нет в AppHost — её нет в реальности.
Несколько облаков могут усилить эту идею.
Например:
*/
// Trading Cloud
var ordersApi = builder.AddProject<Projects.Trading_Api_Orders>("orders-api");
var tradingGateway = builder.AddProject<Projects.Trading_Gateway_Trading>("trading-gateway")
    .WithReference(ordersApi);

// MarketData Cloud
var marketDataApi = builder.AddProject<Projects.Trading_Api_MarketData>("market-data-api");
var marketDataGateway = builder.AddProject<Projects.Trading_Gateway_MarketData>("marketdata-gateway")
    .WithReference(marketDataApi);
