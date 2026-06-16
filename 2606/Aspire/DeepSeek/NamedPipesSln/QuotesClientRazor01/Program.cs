using QuotesClientRazor.Hubs;
using QuotesClientRazor01.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем конфигурацию
builder.Configuration.AddJsonFile("appsettings.json", optional: false);

// Добавляем сервисы
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Регистрируем наши сервисы
builder.Services.AddSingleton<QuoteCache>();
builder.Services.AddHostedService<QuoteClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<QuoteHub>("/quoteHub");

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
