var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

var pipeServer = builder.AddProject<Projects.QuotesApp_PipeServer>("pipeserver")
    .WithReference(redis)
    .WaitFor(redis);

var apiService = builder.AddProject<Projects.QuotesApp_ApiService>("apiservice")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.QuotesApp_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(redis)
    .WaitFor(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
