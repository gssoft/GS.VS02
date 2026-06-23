// ProducerCell.cs - ячейка-производитель событий
// using EventDataFlow.Core.Models;

public class ProducerCell : FractalCell
{
    private int _counter = 0;

    public ProducerCell(
        string cellId,
        CellAddress address,
        IFractalEventHub fractalHub,
        ILogger<ProducerCell> logger)
        : base(cellId, address, fractalHub, logger)
    {
    }

    protected override void ConfigureLocalHandlers()
    {
        // Подписываемся на команды извне
        SubscribeLocal<FractalEvent>(OnExternalCommand);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Запускаем фоновую генерацию событий
        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _counter++;
                var data = new { Message = $"Data from {CellId}", Counter = _counter };

                // Отправляем в соседнюю ячейку
                await SendToCellAsync("consumer-1", "DataGenerated", data);

                // Или broadcast всем ячейкам-потребителям
                await BroadcastAsync("Heartbeat", new { CellId, Timestamp = DateTime.UtcNow },
                    cellId => cellId.StartsWith("consumer"));

                await Task.Delay(3000, stoppingToken);
            }
        }, stoppingToken);

        // Основной цикл слушает входящие команды
        await base.ExecuteAsync(stoppingToken);
    }

    private Task OnExternalCommand(FractalEvent fractalEvent)
    {
        Logger.LogInformation("Producer {CellId} received command: {EventType}",
            CellId, fractalEvent.EventType);

        // Можем изменить поведение по команде извне
        return Task.CompletedTask;
    }
}
