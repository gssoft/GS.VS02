// Giga 26.06.04
// Program.cs
// -------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuoteGeneratorWorker.Models;
using QuoteGeneratorWorker.Services;
using QuoteGeneratorWorker.Handlers;



var host = Host.CreateDefaultBuilder(args)
    // Регистрируем наш генератор котировок как фоновый сервис
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<QuoteGeneratorService>();

        // Регистрируем обработчик, который будет выводить котировки в консоль
        services.AddTransient<IQuoteHandler, ConsoleQuoteHandler>();
    })
    .Build();

await host.RunAsync();
