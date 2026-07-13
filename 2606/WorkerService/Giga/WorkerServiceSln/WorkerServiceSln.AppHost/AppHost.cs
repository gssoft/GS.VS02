//var builder = DistributedApplication.CreateBuilder(args);

//builder.AddProject<Projects.WorkerService01>("workerservice01");

//builder.Build().Run();

// using MassTransit.Transports.InMemory.Fabric;

var builder = DistributedApplication.CreateBuilder(args);

// Ресурс общего пользования (очередь). 
// В реальном проекте здесь был бы AddRedis() или AddRabbitMQ().
// Мы просто регистрируем объект, который внедрят оба сервиса через DI.
var sharedQueue = builder.AddResource(new InMemoryQueue(), "shared-queue");

// Наше веб-приложение
var api = builder.AddProject<Projects.ApiService>("api")
                 .WithReference(sharedQueue); // Передаем ссылку на очередь

// Наш фоновый воркер
var worker = builder.AddProject<Projects.BackgroundProcessor>("worker")
                    .WithReference(sharedQueue); // И ему тоже передаем ту же очередь

builder.Build().Run();