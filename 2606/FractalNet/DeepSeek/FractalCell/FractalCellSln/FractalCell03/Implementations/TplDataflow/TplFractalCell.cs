// Implementations/TplDataflow/TplFractalCell.cs
using System.Threading.Tasks.Dataflow;

public class TplFractalCell : FractalCellTemplate<TplInternalBus, TplExternalBus>
{
    private readonly BufferBlock<IApplicationEvent> _inputBuffer;
    private readonly TransformBlock<IApplicationEvent, IApplicationEvent> _mainProcessingBlock;
    private readonly BroadcastBlock<IApplicationEvent> _broadcastBlock;
    private readonly List<ActionBlock<IApplicationEvent>> _workerBlocks = new();
    private readonly List<IDisposable> _links = new();

    public TplFractalCell(
        FractalCellConfiguration configuration,
        TplInternalBus internalBus,
        TplExternalBus externalBus,
        ILogger<TplFractalCell> logger)
        : base(configuration, internalBus, externalBus, logger)
    {
        var options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = configuration.BusSettings.Capacity,
            MaxDegreeOfParallelism = configuration.BusSettings.MaxParallelism,
            NameFormat = $"{configuration.CellId}-block-{{0}}"
        };

        _inputBuffer = new BufferBlock<IApplicationEvent>(options);

        _mainProcessingBlock = new TransformBlock<IApplicationEvent, IApplicationEvent>(
            async @event => await ProcessInMainBlockAsync(@event),
            options);

        _broadcastBlock = new BroadcastBlock<IApplicationEvent>(
            e => e,
            new DataflowBlockOptions
            {
                BoundedCapacity = configuration.BusSettings.Capacity
            });

        // Связываем блоки с PropagateCompletion = true
        _links.Add(_inputBuffer.LinkTo(_mainProcessingBlock,
            new DataflowLinkOptions { PropagateCompletion = true }));

        _links.Add(_mainProcessingBlock.LinkTo(_broadcastBlock,
            new DataflowLinkOptions { PropagateCompletion = true }));
    }

    private async Task<IApplicationEvent> ProcessInMainBlockAsync(IApplicationEvent @event)
    {
        Logger.LogInformation("Main block processing event {EventId}", @event.EventId);

        try
        {
            // Основная обработка
            await InternalBus.PublishAsync(@event);

            // Отправка во внешний мир
            if (@event is FractalEvent fractalEvent &&
                !string.IsNullOrEmpty(fractalEvent.TargetCellId))
            {
                await ExternalBus.SendToCellAsync(
                    fractalEvent.TargetCellId, fractalEvent);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in main block processing event {EventId}",
                @event.EventId);
            // Здесь можно добавить политику повторных попыток
        }

        return @event;
    }

    protected override IEnumerable<BackgroundService> CreateBackgroundServices()
    {
        for (int i = 0; i < Configuration.BackgroundServiceCount; i++)
        {
            var workerBlock = new ActionBlock<IApplicationEvent>(
                async @event => await ProcessInWorkerAsync(@event, i),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = Configuration.BusSettings.Capacity,
                    MaxDegreeOfParallelism = 1,
                    NameFormat = $"{Configuration.CellId}-worker-{i}"
                });

            // Подключаем воркер к broadcast
            _links.Add(_broadcastBlock.LinkTo(workerBlock,
                new DataflowLinkOptions { PropagateCompletion = true }));

            _workerBlocks.Add(workerBlock);

            yield return new TplBackgroundService(workerBlock,
                $"{Configuration.CellId}-worker-{i}", Logger);
        }
    }

    protected override void ConfigureHandlers()
    {
        // Подписываемся на события внутренней шины
        InternalBus.Subscribe<FractalEvent>(async fractalEvent =>
        {
            await _inputBuffer.SendAsync(fractalEvent);
        });
    }

    private async Task ProcessInWorkerAsync(IApplicationEvent @event, int workerId)
    {
        Logger.LogInformation("Worker {WorkerId} processing event {EventId}",
            workerId, @event.EventId);

        if (@event is FractalEvent fractalEvent)
        {
            try
            {
                switch (fractalEvent.EventType)
                {
                    case "ProcessData":
                        await HandleProcessData(fractalEvent, workerId);
                        break;
                    case "Heartbeat":
                        await HandleHeartbeat(fractalEvent, workerId);
                        break;
                    default:
                        ILogger.LogWarning("Unknown event type: {EventType}",
                            fractalEvent.EventType);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Worker {WorkerId} error processing event {EventId}",
                    workerId, @event.EventId);
            }
        }
    }

    private Task HandleProcessData(FractalEvent @event, int workerId)
    {
        ILogger.LogInformation(
            "Worker {WorkerId} processing data from {Source}: {Payload}",
            workerId, @event.SourceCellId, @event.Payload);

        // Имитация работы
        return Task.Delay(100);
    }

    private Task HandleHeartbeat(FractalEvent @event, int workerId)
    {
        ILogger.LogDebug(
            "Worker {WorkerId} heartbeat from {Source}",
            workerId, @event.SourceCellId);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping TPL blocks...");

        // Инициируем завершение только у источника
        _inputBuffer.Complete();

        try
        {
            // Ждем завершения всей цепочки
            await Task.WhenAll(
                _inputBuffer.Completion,
                _mainProcessingBlock.Completion,
                _broadcastBlock.Completion,
                Task.WhenAll(_workerBlocks.Select(b => b.Completion))
            ).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Stop operation canceled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while stopping TPL blocks");
        }

        // Очищаем ресурсы
        foreach (var link in _links)
        {
            link.Dispose();
        }
        _links.Clear();

        await base.StopAsync(cancellationToken);
    }
}