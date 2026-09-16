using Microsoft.AspNetCore.SignalR.Client;
using TradeSystem.Contracts;

namespace TradeSystem.Web.Services;

public class SignalRClientService : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Action<PositionSnapshot>? OnPositionUpdated;
    public event Action<TickerQuote>? OnQuoteReceived;
    public event Action<OrderDto>? OnOrderUpdated;
    public event Action<TradeDto>? OnTradeExecuted;
    public event Action<PortfolioSnapshot>? OnPortfolioSnapshot;
    public event Action<bool>? OnConnectionChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_hubConnection is not null && IsConnected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7182/tradehub")
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _hubConnection.On<PositionSnapshot>("PositionUpdated",
            snapshot => OnPositionUpdated?.Invoke(snapshot));

        _hubConnection.On<TickerQuote>("QuoteReceived",
            quote => OnQuoteReceived?.Invoke(quote));

        _hubConnection.On<OrderDto>("OrderUpdated",
            order => OnOrderUpdated?.Invoke(order));

        _hubConnection.On<TradeDto>("TradeExecuted",
            trade => OnTradeExecuted?.Invoke(trade));

        _hubConnection.On<PortfolioSnapshot>("PortfolioSnapshot",
            snapshot => OnPortfolioSnapshot?.Invoke(snapshot));

        _hubConnection.Reconnected += _ =>
        {
            OnConnectionChanged?.Invoke(true);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnecting += _ =>
        {
            OnConnectionChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += _ =>
        {
            OnConnectionChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync();
            OnConnectionChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
            OnConnectionChanged?.Invoke(false);
        }
    }

    public async Task SubscribePortfolioAsync(Guid portfolioId)
    {
        if (IsConnected)
            await _hubConnection!.InvokeAsync("SubscribePortfolio", portfolioId);
    }

    public async Task UnsubscribePortfolioAsync(Guid portfolioId)
    {
        if (IsConnected)
            await _hubConnection!.InvokeAsync("UnsubscribePortfolio", portfolioId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}



