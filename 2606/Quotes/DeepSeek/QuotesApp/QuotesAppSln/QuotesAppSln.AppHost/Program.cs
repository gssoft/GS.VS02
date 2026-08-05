var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

var simulatorProvider = builder.AddProject<Projects.QuotesApp_Providers_Simulator>("prov-sim")
    .WithReference(redis)
    .WaitFor(redis);

var externalProvider = builder.AddProject<Projects.QuotesApp_Providers_External>("prov-ext")
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

builder.AddProject<Projects.WorkerService1>("workerservice1");

builder.Build().Run();
