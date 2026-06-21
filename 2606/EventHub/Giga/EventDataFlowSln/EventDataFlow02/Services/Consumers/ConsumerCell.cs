// ConsumerCell.cs - ячейка-потребитель
//  using EventDataFlow.Core.Models;

public class ConsumerCell : FractalCell
{
    private readonly List<object> _processedData = new();

    public ConsumerCell(
        string cellId,
        CellAddress address,
        IFractalEventHub fractalHub,
        ILogger<ConsumerCell> logger)
        : base(cellId, address, fractalHub, logger)
    {
    }

    protected override void ConfigureLocalHandlers()
    {
        SubscribeLocal<FractalEvent>(OnFractalEvent);
    }

    private async Task OnFractalEvent(FractalEvent fractalEvent)
    {
        switch (fractalEvent.EventType)
        {
            case "DataGenerated":
                Logger.LogInformation("Consumer {CellId} processing data from {Source}",
                    CellId, fractalEvent.SourceCellId);

                _processedData.Add(fractalEvent.Payload);

                // Можем ответить отправителю
                await SendToCellAsync(
                    fractalEvent.SourceCellId,
                    "Acknowledgement",
                    new { Received = true, By = CellId }
                );
                break;

            case "Heartbeat":
                Logger.LogDebug("Consumer {CellId} received heartbeat from {Source}",
                    CellId, fractalEvent.SourceCellId);
                break;

            default:
                Logger.LogWarning("Unknown event type: {EventType}", fractalEvent.EventType);
                break;
        }
    }
}