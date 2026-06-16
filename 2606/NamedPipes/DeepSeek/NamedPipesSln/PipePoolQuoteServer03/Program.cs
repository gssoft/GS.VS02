// Program.cs

using QuotesServer.BackgroundServices;
using QuotesServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EventHub>();
builder.Services.AddHostedService<ParallelQuoteServerService>();

var app = builder.Build();

app.MapGet("/", () => @"
<h1>📊 Parallel Quote Server is Running</h1>
<p><strong>Architecture:</strong> Named Pipes IPC Server with Parallel Channels</p>
<p><strong>Active Channels:</strong></p>
<ul>
    <li>Tech Stocks (GOOGL, MSFT, NVDA) - Max 3 parallel clients</li>
    <li>Consumer Stocks (AMZN, AAPL) - Max 3 parallel clients</li>
    <li>Finance Stocks (JPM, BAC, GS) - Max 2 parallel clients</li>
    <li>Energy Stocks (XOM, CVX) - Max 2 parallel clients</li>
</ul>
<p>Check logs for real-time parallel quote distribution.</p>
");

app.Run();
