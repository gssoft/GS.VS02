using Microsoft.AspNetCore.SignalR;
using Trading.Dashboard.Components;
using Trading.Dashboard.Hubs;
using Trading.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR
builder.Services.AddSignalR();

// EventMonitorService как hosted service для подписки на RabbitMQ
builder.Services.AddSingleton<EventMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventMonitorService>());

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Маппинг хаба
app.MapHub<TradingHub>("/hubs/trading");

app.Run();

//using Trading.Dashboard.Components;
//using Trading.Dashboard.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//// Добавьте регистрацию сервиса
//builder.Services.AddSingleton<EventMonitorService>();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}
//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
//app.UseHttpsRedirection();
//app.UseAntiforgery();

//app.MapStaticAssets();
//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();

