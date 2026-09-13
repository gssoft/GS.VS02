// TradeSystem.SignalRHub/Program.cs
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.MapHub<TradeHub>("/tradehub");

app.Run();

public class TradeHub : Hub
{
    // Клиент подключается → получает snapshot, затем push-обновления
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            ConnectedAt = DateTime.UtcNow
        });
        await base.OnConnectedAsync();
    }

    // Подписка на конкретный портфель
    public Task SubscribePortfolio(Guid portfolioId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"portfolio-{portfolioId}");
    }

    // Отписка от портфеля
    public Task UnsubscribePortfolio(Guid portfolioId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"portfolio-{portfolioId}");
    }
}
