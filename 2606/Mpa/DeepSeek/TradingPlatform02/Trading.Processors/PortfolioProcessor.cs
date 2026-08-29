// PortfolioProcessor.cs
using Trading.Core;
using Trading.Domain;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.Processors;

public class PortfolioProcessor : EventDrivenProcessor<PositionUpdated>
{
    private readonly InMemoryDatabase _db;

    public PortfolioProcessor(IMicroEventBus bus, ILoggerFactory loggerFactory, InMemoryDatabase db)
        : base(bus, loggerFactory)
    {
        _db = db;
    }

    protected override async Task HandleAsync(PositionUpdated position, CancellationToken ct)
    {
        // В реальном приложении можно обновлять агрегированный снапшот
        var snapshot = new PortfolioSnapshot(_db.Positions.ToDictionary(p => p.Key, p => p.Value.Quantity));
        await Bus.PublishAsync(snapshot, ct);
        Logger.LogInformation("Portfolio snapshot: {Count} positions", snapshot.Positions.Count);
    }
}
