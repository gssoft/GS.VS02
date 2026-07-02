using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GA.BackgroundServices.Core; // Подключаем пространство имен вашей библиотеки

// Создаем билдер хоста. Это точка входа приложения Worker Service.
var builder = Host.CreateApplicationBuilder(args);

// ---------------------------------------------------
// 1. Настройка конфигурации и логирования (опционально)
// ---------------------------------------------------
builder.Logging.ClearProviders(); // Очищаем провайдеры по умолчанию
builder.Logging.AddConsole();     // Добавляем вывод в консоль (стандарт для воркеров)

// Если у вас есть настройки в appsettings.json специально для этого сервиса:
// builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("CustomDataProcessor"));

// ---------------------------------------------------
// 2. Регистрация зависимостей (DI Container)
// ---------------------------------------------------

// ВАЖНО: CoreWorker из вашей библиотеки уже зарегистрирован как IHostedService внутри себя,
// но если он использует другие сервисы (например, HttpClientFactory или DbContext), 
// их нужно добавить здесь ДО регистрации самого процессора.

// Пример добавления HTTP клиента, если библиотека его использует:
// builder.Services.AddHttpClient();

// Пример настроек времени жизни (Lifetimes):
// - AddSingleton: один экземпляр на всю жизнь приложения.
// - AddScoped: новый экземпляр для каждой итерации цикла DoWorkAsync (безопасно для EF Core DbContext).
// - AddTransient: новый экземпляр при каждом запросе внедрения.

// Регистрируем сторонние компоненты, которые нужны CustomDataProcessor
// builder.Services.AddSingleton<IMyExternalApi, MyExternalApi>();

// ---------------------------------------------------
// 3. Регистрация фонового сервиса
// ---------------------------------------------------
// RegisterHostedService гарантирует, что будет создан только ОДИН экземпляр сервиса за весь цикл жизни приложения.
// Именно этот метод нужен для BackgroundService.
builder.Services.AddHostedService<CustomDataProcessor>();

// Альтернатива (работает так же для BackgroundService):
// builder.Services.AddSingleton<IHostedService, CustomDataProcessor>();

// ---------------------------------------------------
// Сборка и запуск хоста
// ---------------------------------------------------
var host = builder.Build();

await host.RunAsync();
