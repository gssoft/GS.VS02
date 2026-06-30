// Program.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration; // <-- ДОБАВИТЬ using
using QuoteGeneratorWorker.Models;
using QuoteGeneratorWorker.Services;
using QuoteGeneratorWorker.Handlers;
using QuoteGeneratorWorker.Options;
using System.Text; // <-- ДОБАВИТЬ using

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    // Загружаем appsettings.json (и appsettings.Development.json)
    // Это уже делается по умолчанию, но оставим для ясности
});

builder.ConfigureServices((hostContext, services) =>
{
    // --- 1. ЧТЕНИЕ НАСТРОЕК ИЗ ФАЙЛА ---
    // Привязываем секцию "StockQuoteOptions" к нашему классу StockQuoteOptions
    services.Configure<StockQuoteOptions>(hostContext.Configuration.GetSection("StockQuoteOptions"));

    // --- 2. РЕГИСТРАЦИЯ СЕРВИСА И ОБРАБОТЧИКОВ ---
    services.AddHostedService<QuoteGeneratorService>();

    // Регистрируем все реализации IQuoteHandler.
    // AddTransient создаст новый экземпляр для каждого вызова.
    services.AddTransient<IQuoteHandler, ConsoleQuoteHandler>();
    services.AddTransient<IQuoteHandler, DatabaseQuoteHandler>();
    services.AddTransient<IQuoteHandler, ApiQuoteHandler>();
});

var host = builder.Build();
await host.RunAsync();

