using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

using QuoteGeneratorWorker;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // Привязываем секцию "StockQuoteOptions" к нашему классу StockQuoteOptions
    services.Configure<QuoteGeneratorWorker.Options.StockQuoteOptions>(hostContext.Configuration.GetSection("StockQuoteOptions"));

    // Регистрируем наш сервис, который использует TPL Dataflow.
    // Интерфейс IQuoteHandler и его реализации больше не нужны и не регистрируются.
    services.AddHostedService<QuoteGeneratorWorker.Services.QuoteGeneratorService>();
});

var host = builder.Build();
await host.RunAsync();
