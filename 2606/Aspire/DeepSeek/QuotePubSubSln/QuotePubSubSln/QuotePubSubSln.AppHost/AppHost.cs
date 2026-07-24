var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.QuotePubSub>("quotepubsub");

builder.AddProject<Projects.QuoteGeneratorsHost>("quotegeneratorshost");

builder.AddProject<Projects.QuoteClientsHost>("quoteclientshost");

builder.AddProject<Projects.GeneratorTech>("generatortech");

builder.AddProject<Projects.GeneratorFinance>("generatorfinance");

builder.AddProject<Projects.GeneratorEnergy>("generatorenergy");

builder.AddProject<Projects.GeneratorConsumer>("generatorconsumer");

builder.AddProject<Projects.ClientA>("clienta");

builder.AddProject<Projects.ClientB>("clientb");

builder.AddProject<Projects.QuoteModels>("quotemodels");

builder.Build().Run();
