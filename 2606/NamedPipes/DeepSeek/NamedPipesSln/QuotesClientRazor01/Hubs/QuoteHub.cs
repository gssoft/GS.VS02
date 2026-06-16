using Microsoft.AspNetCore.SignalR;
using QuotesClientRazor01.Models;

namespace QuotesClientRazor.Hubs;

public class QuoteHub : Hub
{
    public async Task SubscribeToChannel(string channel)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channel);
    }

    public async Task UnsubscribeFromChannel(string channel)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
    }
}
