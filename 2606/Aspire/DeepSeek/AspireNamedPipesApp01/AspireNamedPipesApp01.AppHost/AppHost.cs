var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireNamedPipesApp01_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireNamedPipesApp01_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.MyPipeService>("mypipeservice");

builder.Build().Run();
