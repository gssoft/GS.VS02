// Program.cs

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
