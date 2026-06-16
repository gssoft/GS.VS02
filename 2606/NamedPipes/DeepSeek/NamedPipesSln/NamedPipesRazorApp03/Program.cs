// Program.cs
using NamedPipes.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов
builder.Services.AddSingleton<EventHub>();
builder.Services.AddHostedService<QuoteServerService>();
builder.Services.AddHostedService<FirstSubscriberService>();
builder.Services.AddHostedService<SecondSubscriberService>();

var app = builder.Build();

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
