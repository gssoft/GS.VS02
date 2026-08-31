using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.Core;
using Trading.Processors;
using Trading.Storage;

namespace Trading.App;

public static class ManifestProcessorFactory
{
    public static object CreateProcessor(
        string type,
        IMicroEventBus bus,
        ILoggerFactory loggerFactory,
        InMemoryDatabase db,
        Dictionary<string, object>? config)
    {
        return type switch
        {
            nameof(QuotesFeederProcessor) => CreateQuotesFeeder(bus, loggerFactory, config),
            nameof(StrategyProcessor) => CreateStrategy(bus, loggerFactory, db, config),
            nameof(OrderExecutorProcessor) => CreateOrderExecutor(bus, loggerFactory, db, config),
            nameof(TradeProcessor) => new TradeProcessor(bus, loggerFactory, db),
            nameof(PositionProcessor) => new PositionProcessor(bus, loggerFactory, db),
            nameof(PortfolioProcessor) => new PortfolioProcessor(bus, loggerFactory, db),
            nameof(EventLogProcessor) => new EventLogProcessor(bus, db, loggerFactory.CreateLogger<EventLogProcessor>()),
            _ => throw new NotSupportedException($"Processor type '{type}' is not supported.")
        };
    }

    private static QuotesFeederProcessor CreateQuotesFeeder(
        IMicroEventBus bus,
        ILoggerFactory loggerFactory,
        Dictionary<string, object>? config)
    {
        string[] tickers = config != null && config.TryGetValue("tickers", out var t) && t is JsonElement arr && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray().Select(e => e.GetString()!).ToArray()
            : new[] { "AAPL", "MSFT", "GOOGL" };

        return new QuotesFeederProcessor(bus, loggerFactory, tickers);
    }

    private static StrategyProcessor CreateStrategy(
        IMicroEventBus bus,
        ILoggerFactory loggerFactory,
        InMemoryDatabase db,
        Dictionary<string, object>? config)
    {
        decimal lotSize = 10m;
        if (config != null && config.TryGetValue("lotSize", out var l) && l is JsonElement je && je.ValueKind == JsonValueKind.Number)
        {
            lotSize = je.GetDecimal();
        }

        return new StrategyProcessor(bus, loggerFactory, db, lotSize);
    }

    private static OrderExecutorProcessor CreateOrderExecutor(
        IMicroEventBus bus,
        ILoggerFactory loggerFactory,
        InMemoryDatabase db,
        Dictionary<string, object>? config)
    {
        double fillProbability = 0.7;
        if (config != null && config.TryGetValue("fillProbability", out var p) && p is JsonElement je && je.ValueKind == JsonValueKind.Number)
        {
            fillProbability = je.GetDouble();
        }

        return new OrderExecutorProcessor(bus, loggerFactory, db, fillProbability);
    }
}
