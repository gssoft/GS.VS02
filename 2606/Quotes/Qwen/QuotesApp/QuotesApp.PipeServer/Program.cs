using QuotesApp.PipeServer.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cache");

builder.Services.AddHostedService<QuotePublisherService>();

var host = builder.Build();
host.Run();
