using Trading.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Регистрируем HttpClient, который будет ходить в Gateway.
// "https+http://gateway" — это не реальный URL, а логическое имя,
// которое Aspire разрешит в конкретный адрес через Service Discovery.
builder.Services.AddHttpClient<TradingApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://gateway");
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


//using Trading.Web;
//using Trading.Web.Components;

//var builder = WebApplication.CreateBuilder(args);

//// Add service defaults & Aspire client integrations.
//builder.AddServiceDefaults();
//builder.AddRedisOutputCache("cache");

//// Add services to the container.
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//    {
//        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
//        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
//        client.BaseAddress = new("https+http://apiservice");
//    });

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

//app.UseAntiforgery();

//app.UseOutputCache();

//app.MapStaticAssets();

//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.MapDefaultEndpoints();

//app.Run();
