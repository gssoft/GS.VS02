using Quotes.Shared.Contracts;
using System.Threading.Channels;

using AspireApp.ApiService.Service;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Регистрируем канал для передачи котировок внутри процесса
builder.Services.AddSingleton(Channel.CreateUnbounded<QuoteDto>());

// Регистрируем наш генератор как фоновую службу
builder.Services.AddHostedService<QuotesGeneratorService>();

// Добавляем эндпоинт, чтобы отдавать данные Web-у
builder.Services.AddControllers();

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapControllers(); // Для API endpoint

app.Run();


