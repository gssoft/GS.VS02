using TradeSystem.Web;
using TradeSystem.Web.Components;
using TradeSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClientService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7527");
});

builder.Services.AddScoped<SignalRClientService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


//using TradeSystem.Web;
//using TradeSystem.Web.Components;
//using TradeSystem.Web.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<ApiClientService>(client =>
//{
//    client.BaseAddress = new Uri("https://localhost:7527");
//});

//builder.Services.AddScoped<SignalRClientService>();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//app.UseAntiforgery();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();



//using TradeSystem.Web;
//using TradeSystem.Web.Components;
//using TradeSystem.Web.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<ApiClientService>(client =>
//{
//    client.BaseAddress = new Uri("https://localhost:7527");
//});

//builder.Services.AddScoped<SignalRClientService>();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//app.UseAntiforgery();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();


//using TradeSystem.Web;

//using TradeSystem.Web.Components;
//using TradeSystem.Web.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<ApiClientService>(client =>
//{
//    client.BaseAddress = new Uri("https://localhost:7527");
//});

//builder.Services.AddScoped<SignalRClientService>();

//var app = builder.Build();

//app.MapDefaultEndpoints();

//app.UseAntiforgery();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();


//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using TradeSystem.Web;
//using TradeSystem.Web.Services;

//var builder = WebAssemblyHostBuilder.CreateDefault(args);

//builder.Services.AddAuthorizationCore();
//builder.Services.AddCascadingAuthenticationState();

//// Регистрация сервисов
//builder.Services.AddScoped<SignalRClientService>();
//builder.Services.AddHttpClient<ApiClientService>(client =>
//{
//    client.BaseAddress = new Uri("https://localhost:7527");
//});

//await builder.Build().RunAsync();


