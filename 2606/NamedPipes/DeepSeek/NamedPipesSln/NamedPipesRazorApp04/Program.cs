using MyApp.Services;

// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Регистрация компонентов
builder.Services.AddSingleton<EventHub>(); // EventHub регистрируется как Singleton
builder.Services.AddHostedService<QuoteServerService>(); // Главный сервис котировок
builder.Services.AddHostedService<FirstSubscriberService>(); // Потребитель GOOGL, MSFT, NVDA
builder.Services.AddHostedService<SecondSubscriberService>(); // Потребитель AMZN, AAPL

var app = builder.Build();

// Простое отображение состояния
app.MapGet("/", () => "Interprocess communication services are running in background. Check logs or connect to pipes.");

app.Run();

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

//app.UseRouting();

//app.UseAuthorization();

//app.MapStaticAssets();
//app.MapRazorPages()
//   .WithStaticAssets();

//app.Run();
