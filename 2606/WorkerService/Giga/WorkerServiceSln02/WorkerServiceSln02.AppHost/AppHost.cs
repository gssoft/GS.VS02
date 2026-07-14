//var builder = DistributedApplication.CreateBuilder(args);

//// var sharedQueue = builder.AddResource(new InMemoryQueue(), "shared-queue");  <- не компилируется

//builder.AddProject<Projects.WorkerService02>("workerservice02");

//builder.AddProject<Projects.ApiService>("apiservice");

//builder.Build().Run();
// --------------------------------------------------

// WorkerServiceSln02 / AppHost.cs

//var builder = DistributedApplication.CreateBuilder(args);

//// Регистрируем общий ресурс "Очередь". 
//// Параметр 'factory' говорит Aspire создавать этот объект внутри контейнера DI хоста.
//// Теперь это будет один и тот же экземпляр InMemoryQueue для всех сервисов.
//var sharedQueue = builder.AddResource("shared-queue", factory => new InMemoryQueue());

//// Наш фоновый воркер
//builder.AddProject<Projects.WorkerService02>("workerservice02")
//       .WithReference(sharedQueue); // Передаем ссылку на общую очередь

//// Наше веб-приложение
//builder.AddProject<Projects.ApiService>("apiservice")
//       .WithReference(sharedQueue); // И ему тоже передаем ту же самую очередь

//builder.Build().Run();

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Создаем экземпляр очереди прямо здесь, в DI-контейнере AppHost.
// Это гарантирует, что будет создан ровно один объект InMemoryQueue.
builder.Services.AddSingleton<IMessageQueue, InMemoryQueue>();

// 2. Регистрируем воркер. Ему НЕ нужен WithReference() для передачи объекта,
// так как он получит очередь напрямую из контейнера DI этого же хоста.
builder.AddProject<Projects.WorkerService02>("workerservice02");

// 3. Регистрируем API. 
// Мы используем AddParameter(), чтобы "пробросить" имя ресурса очереди внутрь ApiService.
// Внутри Program.cs ApiService мы прочитаем это имя и достанем тот самый синглтон из своего DI.
var queueParam = builder.AddParameter("queue-name", "shared-queue");

builder.AddProject<Projects.ApiService>("apiservice")
       .WithEnvironment("QUEUE_NAME", queueParam); // Передаем имя как переменную среды

builder.Build().Run();

