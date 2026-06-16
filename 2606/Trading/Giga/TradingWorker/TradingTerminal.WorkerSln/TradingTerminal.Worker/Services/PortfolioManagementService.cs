// Services/PortfolioManagementService.cs

using System.Collections.Generic;
using TradingTerminal.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TradingTerminal.Services;

public class PortfolioManagementService
{
    private readonly Dictionary<string, int> _portfolio = new();
    private readonly ILogger<PortfolioManagementService> _logger;

    public PortfolioManagementService(ILogger<PortfolioManagementService> logger)
    {
        _logger = logger;
    }

    public Task ProcessDataAsync(object data)
    {
        if (data is Trade trade)
        {
            if (_portfolio.ContainsKey(trade.Symbol))
                _portfolio[trade.Symbol] += trade.Volume;
            else
                _portfolio[trade.Symbol] = trade.Volume;

            _logger.LogInformation($"[PORTFOLIO] Позиция по {trade.Symbol} обновлена: {_portfolio[trade.Symbol]} лотов");
        }
        return Task.CompletedTask;
    }
}
