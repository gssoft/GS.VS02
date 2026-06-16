// Services/OrderExecutionService.cs

using System.Collections.Generic;
using System.Linq;
using TradingTerminal.Models;
using System.Threading.Tasks;

namespace TradingTerminal.Services;

public class OrderExecutionService
{
    // В реальном приложении это был бы репозиторий или кэш
    private readonly List<LimitOrder> _activeOrders = new()
    {
        new LimitOrder { LimitPrice = 152.50m, Volume = 100 },
        new LimitOrder { LimitPrice = 153.00m, Volume = 50 }
    };

    // Этот метод будет передан в ActionBlock как делегат
    public Task ProcessDataAsync(object data)
    {
        if (data is Quote quote)
        {
            var ordersToExecute = _activeOrders.Where(o => quote.Price >= o.LimitPrice).ToList();
            foreach (var order in ordersToExecute)
            {
                Console.WriteLine($"[ENGINE] Исполнен ордер на {order.Volume} лотов по цене {quote.Price:C2}");
                _activeOrders.Remove(order);
            }
        }
        return Task.CompletedTask;
    }
}
