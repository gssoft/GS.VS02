// Program.cs

using EmailService01;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Создаем билдер хоста с настройками по умолчанию (логирование, конфигурация из appsettings.json)
var builder = Host.CreateDefaultBuilder(args);

// Конфигурируем сервисы, которые будут доступны через Dependency Injection
builder.ConfigureServices((hostContext, services) =>
{
    // Регистрируем нашу реализацию IEmailSender как Singleton.
    // Это значит, что на все время работы приложения будет создан только один экземпляр ConsoleEmailSender.
    services.AddSingleton<IEmailSender, ConsoleEmailSender>();

    // Добавляем наш фоновый сервис в коллекцию хостов.
    // Фреймворк сам создаст экземпляр MyWorker и внедрит в него зависимости.
    services.AddHostedService<MyWorker>();
});

// Строим и запускаем приложение.
// Этот вызов блокирует основной поток до остановки приложения (например, Ctrl+C).
var host = builder.Build();
await host.RunAsync();
