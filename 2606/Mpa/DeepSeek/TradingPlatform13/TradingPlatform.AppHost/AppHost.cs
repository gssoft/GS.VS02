var builder = DistributedApplication.CreateBuilder(args);

var tradingApp = builder.AddProject<Projects.Trading_App>("trading-app");
var dashboard = builder.AddProject<Projects.Trading_Dashboard>("trading-dashboard");

// Добавьте RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq");
tradingApp.WithReference(rabbitmq);
dashboard.WithReference(rabbitmq);

builder.Build().Run();

//var builder = DistributedApplication.CreateBuilder(args);

//builder.AddProject<Projects.Trading_Dashboard>("trading-dashboard");

//builder.Build().Run();
