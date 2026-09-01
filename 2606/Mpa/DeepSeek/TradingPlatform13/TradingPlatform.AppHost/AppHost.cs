var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Trading_Dashboard>("trading-dashboard");

builder.Build().Run();
