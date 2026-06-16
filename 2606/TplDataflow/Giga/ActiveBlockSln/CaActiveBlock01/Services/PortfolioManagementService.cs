// Services/PortfolioManagementService.cs

using System.Collections.Generic;
using TradingTerminal.Models;
using System.Threading.Tasks;

namespace TradingTerminal.Services;

public class PortfolioManagementService
{
    private readonly Dictionary<string, int> _portfolio = new();

    // Этот метод будет передан в ActionBlock как делегат
    public Task ProcessDataAsync(object data)
    {
        if (data is Trade trade)
        {
            if (_portfolio.ContainsKey(trade.Symbol))
                _portfolio[trade.Symbol] += trade.Volume;
            else
                _portfolio[trade.Symbol] = trade.Volume;

            Console.WriteLine($"[PORTFOLIO] Позиция по {trade.Symbol} обновлена: {_portfolio[trade.Symbol]} лотов");
        }
        return Task.CompletedTask;
    }
}
