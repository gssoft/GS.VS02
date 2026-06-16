using NamedPipes.Services;
using NamedPipes.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton<IPublisherService>(sp => sp.GetRequiredService<EventHub>());
builder.Services.AddSingleton<ISubscriberService>(sp => sp.GetRequiredService<EventHub>());

// Background Services
builder.Services.AddHostedService<QuoteServerService>();
builder.Services.AddHostedService<FirstSubscriberService>();
builder.Services.AddHostedService<SecondSubscriberService>();

var app = builder.Build();

app.MapGet("/", () => @"
<h1>📊 Quote Server is Running</h1>
<p><strong>Architecture:</strong> Named Pipes IPC</p>
<p><strong>QuoteServer:</strong> IPC Server (Publishes all tickers)</p>
<p><strong>FirstSubscriber:</strong> IPC Client (GOOGL, MSFT, NVDA)</p>
<p><strong>SecondSubscriber:</strong> IPC Client (AMZN, AAPL)</p>
<p>Check logs for real-time quote distribution.</p>
");

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
