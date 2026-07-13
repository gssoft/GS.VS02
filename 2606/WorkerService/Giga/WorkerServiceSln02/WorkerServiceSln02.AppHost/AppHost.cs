var builder = DistributedApplication.CreateBuilder(args);

// var sharedQueue = builder.AddResource(new InMemoryQueue(), "shared-queue");

builder.AddProject<Projects.WorkerService02>("workerservice02");

builder.AddProject<Projects.ApiService>("apiservice");

builder.Build().Run();
