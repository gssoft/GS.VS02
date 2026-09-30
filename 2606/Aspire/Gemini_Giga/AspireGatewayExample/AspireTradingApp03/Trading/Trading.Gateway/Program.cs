using Microsoft.AspNetCore.Builder;
using Yarp.ReverseProxy.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.AspNetCore.Http;   // ← может понадобиться

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapReverseProxy();

// Диагностика: покажет все загруженные маршруты и кластеры
app.MapGet("/debug/routes", (IProxyConfigProvider configProvider) =>
{
    var config = configProvider.GetConfig();
    return Results.Ok(new
    {
        Routes = config.Routes.Select(r => new { r.RouteId, Path = r.Match.Path, r.ClusterId }),
        Clusters = config.Clusters.Select(c => new
        {
            c.ClusterId,
            Destinations = c.Destinations?.Select(d => new { d.Key, d.Value.Address })
        })
    });
});

app.Run();

//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.ServiceDiscovery;   // ← может понадобиться

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults(); // OpenTelemetry, service discovery, resilience

//builder.Services.AddReverseProxy()
//    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
//    .AddServiceDiscoveryDestinationResolver();   // ← это обязательно

//// Настраиваем YARP как обратный прокси
////builder.Services.AddReverseProxy()
////    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

//var app = builder.Build();

//app.MapDefaultEndpoints(); // /health, /alive
//app.MapReverseProxy();

//app.Run();
