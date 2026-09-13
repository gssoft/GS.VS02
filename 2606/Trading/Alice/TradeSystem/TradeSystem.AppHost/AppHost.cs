// TradeSystem.AppHost/Program.cs

using Microsoft.AspNetCore.SignalR;

var builder = DistributedApplication.CreateBuilder(args);

// Существующие сервисы
var apiService = builder.AddProject<Projects.TradeSystem_ApiService>("apiservice");

// SignalR Hub
var signalrHub = builder.AddProject<Projects.TradeSystem_SignalRHub>("signalrhub")
    .WithReference(apiService);

// WebClient
builder.AddProject<Projects.TradeSystem_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(signalrHub);

builder.Build().Run();

