var builder = DistributedApplication.CreateBuilder(args);

// Подключаемся к уже запущенному RabbitMQ через строку подключения
var rabbitMq = builder.AddConnectionString("rabbitmq", "amqp://guest:guest@localhost:5672");

builder.AddProject<Projects.GeneratorTech>("generator-tech")
    .WithReference(rabbitMq);

builder.Build().Run();
