using QuotesServer.BackgroundServices;
using QuotesServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EventHub>();
builder.Services.AddHostedService<QuoteServerService>();

var app = builder.Build();

app.MapGet("/", () => @"
<h1>📊 Quote Server is Running</h1>
<p><strong>Architecture:</strong> Named Pipes IPC Server</p>
<p><strong>Channels:</strong></p>
<ul>
    <li>First Channel (GOOGL, MSFT, NVDA) → \\.\pipe\first-subscriber-channel</li>
    <li>Second Channel (AMZN, AAPL) → \\.\pipe\second-subscriber-channel</li>
</ul>
<p>Check logs for real-time quote distribution.</p>
");

app.Run();
