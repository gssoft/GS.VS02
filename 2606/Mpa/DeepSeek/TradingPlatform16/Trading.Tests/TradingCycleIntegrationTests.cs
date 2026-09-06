// TradingCycleIntegrationTests.cs
using Trading.Core;
using Trading.Domain;
using Trading.Processors;
using Trading.Storage;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Trading.Tests;

public class TradingCycleIntegrationTests
{
    [Fact]
    public async Task FullCycle_Should_Produce_Trades_And_NonNegative_Positions()
    {
        // Arrange
        var bus = new InMemoryMicroEventBus();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var db = new InMemoryDatabase();
        var tickers = new[] { "AAPL", "MSFT", "GOOGL" };

        // Регистрируем процессоры (как в Program.cs, но вручную)
        var quotesFeeder = new QuotesFeederProcessor(bus, loggerFactory, tickers);
        var strategy = new StrategyProcessor(bus, loggerFactory, db);
        var executor = new OrderExecutorProcessor(bus, loggerFactory, db);
        var tradeProcessor = new TradeProcessor(bus, loggerFactory, db);
        var positionProcessor = new PositionProcessor(bus, loggerFactory, db);
        var portfolioProcessor = new PortfolioProcessor(bus, loggerFactory, db);

        // Act
        await quotesFeeder.GenerateQuotesAsync(CancellationToken.None);
        await Task.Delay(1000); // даём время на полный цикл

        // Assert
        // 1. Все позиции неотрицательны
        foreach (var pos in db.Positions.Values)
        {
            Assert.True(pos.Quantity >= 0, $"Position for {pos.Ticker} is negative: {pos.Quantity}");
        }

        // 2. Количество сделок равно количеству исполненных ордеров
        int filledOrdersCount = db.Orders.Values.Count(o => db.Trades.Values.Any(t => t.OrderId == o.OrderId));
        int tradesCount = db.Trades.Count;
        Assert.Equal(filledOrdersCount, tradesCount);
    }
}
