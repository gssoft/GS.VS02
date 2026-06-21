// RouterCell.cs - ячейка-маршрутизатор (черная дыра)

public class RouterCell : FractalCell
{
    private readonly Dictionary<string, List<string>> _routingTable = new();
    private readonly IFractalEventHub _childHub;

    public RouterCell(
        string cellId,
        CellAddress address,
        IFractalEventHub fractalHub,
        ILogger<RouterCell> logger)
        : base(cellId, address, fractalHub, logger)
    {
        // Создаем дочерний хаб для вложенных ячеек
        _childHub = new UniversalFractalEventHub(
            logger as ILogger<UniversalFractalEventHub> ??
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<UniversalFractalEventHub>(),
            $"{cellId}-child-hub",
            fractalHub // родительский хаб
        );
    }

    protected override void ConfigureLocalHandlers()
    {
        SubscribeLocal<FractalEvent>(OnRoutingEvent);
    }

    public void AddRoute(string eventType, string targetCellId)
    {
        if (!_routingTable.ContainsKey(eventType))
        {
            _routingTable[eventType] = new List<string>();
        }
        _routingTable[eventType].Add(targetCellId);
    }

    private async Task OnRoutingEvent(FractalEvent fractalEvent)
    {
        Logger.LogInformation("Router {CellId} routing event: {EventType}",
            CellId, fractalEvent.EventType);

        // Проверяем таблицу маршрутизации
        if (_routingTable.TryGetValue(fractalEvent.EventType, out var targets))
        {
            foreach (var target in targets)
            {
                // Можем маршрутизировать в дочерний хаб
                if (target.StartsWith($"{CellId}/"))
                {
                    var childCellId = target.Substring(CellId.Length + 1);
                    await _childHub.PublishAsync(childCellId, fractalEvent);
                }
                else
                {
                    // Или во внешний мир через родительский хаб
                    await SendToCellAsync(target, fractalEvent.EventType, fractalEvent.Payload);
                }
            }
        }
        else
        {
            // По умолчанию - broadcast всем известным
            await BroadcastAsync(fractalEvent.EventType, fractalEvent.Payload);
        }
    }

    public IFractalEventHub GetChildHub() => _childHub;
}
