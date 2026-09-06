var builder = DistributedApplication.CreateBuilder(args);

// Добавьте RabbitMQ с healthcheck
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()  // Добавляет management UI
    .WithLifetime(ContainerLifetime.Persistent);  // Контейнер не удаляется при остановке

var tradingApp = builder.AddProject<Projects.Trading_App>("trading-app")
    .WithReference(rabbitmq);

var dashboard = builder.AddProject<Projects.Trading_Dashboard>("trading-dashboard")
    .WithReference(rabbitmq);

builder.Build().Run();

//var builder = DistributedApplication.CreateBuilder(args);

//var tradingApp = builder.AddProject<Projects.Trading_App>("trading-app");
//var dashboard = builder.AddProject<Projects.Trading_Dashboard>("trading-dashboard");

//// Добавьте RabbitMQ
//var rabbitmq = builder.AddRabbitMQ("rabbitmq");
//tradingApp.WithReference(rabbitmq);
//dashboard.WithReference(rabbitmq);

//builder.Build().Run();

