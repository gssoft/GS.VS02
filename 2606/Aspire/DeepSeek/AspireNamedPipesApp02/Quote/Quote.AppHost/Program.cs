var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.QuoteServer>("quoteserver")
    .WithEnvironment("SERVER_PORT", "5555");

// Передаём клиентам адрес сервера (внутри Aspire все видят друг друга по именам)
builder.AddProject<Projects.QuoteClient1>("quoteclient1")
    .WithEnvironment("SERVER_HOST", "quoteserver")
    .WithEnvironment("SERVER_PORT", "5555");

builder.AddProject<Projects.QuoteClient2>("quoteclient2")
    .WithEnvironment("SERVER_HOST", "quoteserver")
    .WithEnvironment("SERVER_PORT", "5555");

builder.Build().Run();
