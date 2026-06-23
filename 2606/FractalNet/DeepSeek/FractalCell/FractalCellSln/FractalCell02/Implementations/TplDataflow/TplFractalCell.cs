// Implementations/TplDataflow/TplFractalCell.cs

using System.Threading.Tasks.Dataflow;

public class TplFractalCell : FractalCellTemplate<TplInternalBus, TplExternalBus>
{
    private readonly List<BackgroundService> _services = new();
    private readonly BufferBlock<IApplicationEvent> _inputBuffer;        // ← Входной буфер
    private readonly TransformBlock<IApplicationEvent, IApplicationEvent> _mainProcessingBlock;  // ← TransformBlock
    private readonly BroadcastBlock<IApplicationEvent> _broadcastBlock;
    private readonly List<ActionBlock<IApplicationEvent>> _workerBlocks = new();

    public TplFractalCell(
        CellConfiguration configuration,
        TplInternalBus internalBus,
        TplExternalBus externalBus,
        ILogger<TplFractalCell> logger)
        : base(configuration, internalBus, externalBus, logger)
    {
        // Входной буфер для приема событий
        _inputBuffer = new BufferBlock<IApplicationEvent>(
            new DataflowBlockOptions
            {
                BoundedCapacity = configuration.ChannelCapacity,
                NameFormat = $"{configuration.CellId}-input"
            });

        // TransformBlock может быть и приемником и источником
        _mainProcessingBlock = new TransformBlock<IApplicationEvent, IApplicationEvent>(
            async @event => await ProcessInMainBlockAsync(@event),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = configuration.ChannelCapacity,
                MaxDegreeOfParallelism = configuration.MaxParallelism,
                NameFormat = $"{configuration.CellId}-main"
            });

        // Broadcast для распределения событий между воркерами
        _broadcastBlock = new BroadcastBlock<IApplicationEvent>(
            e => e,
            new DataflowBlockOptions
            {
                BoundedCapacity = configuration.ChannelCapacity,
                NameFormat = $"{configuration.CellId}-broadcast"
            });

        // Правильная цепочка: InputBuffer → TransformBlock → BroadcastBlock
        _inputBuffer.LinkTo(_mainProcessingBlock,
            new DataflowLinkOptions { PropagateCompletion = true });

        _mainProcessingBlock.LinkTo(_broadcastBlock,
            new DataflowLinkOptions { PropagateCompletion = true });
    }

    private async Task<IApplicationEvent> ProcessInMainBlockAsync(IApplicationEvent @event)
    {
        Logger.LogInformation("Main block processing event {EventId}", @event.EventId);

        // Основная обработка
        await InternalBus.PublishAsync(@event);

        // Если нужно отправить во внешний мир
        if (@event is FractalEvent fractalEvent &&
            !string.IsNullOrEmpty(fractalEvent.TargetCellId))
        {
            await ExternalBus.SendToCellAsync(
                fractalEvent.TargetCellId, fractalEvent);
        }

        // Возвращаем событие для дальнейшей цепочки
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
                    BoundedCapacity = Configuration.ChannelCapacity,
                    MaxDegreeOfParallelism = 1,
                    NameFormat = $"{Configuration.CellId}-worker-{i}"
                });

            // Подключаем воркер к broadcast
            _broadcastBlock.LinkTo(workerBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            _workerBlocks.Add(workerBlock);

            yield return new TplBackgroundService(workerBlock,
                $"{Configuration.CellId}-worker-{i}", Logger);
        }
    }

    protected override void ConfigureHandlers()
    {
        InternalBus.Subscribe<FractalEvent>(async fractalEvent =>
        {
            // Отправляем во входной буфер
            await _inputBuffer.SendAsync(fractalEvent);
        });
    }

    private async Task ProcessInWorkerAsync(IApplicationEvent @event, int workerId)
    {
        Logger.LogInformation("Worker {WorkerId} processing event {EventId}",
            workerId, @event.EventId);

        if (@event is FractalEvent fractalEvent)
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
                    Logger.LogWarning("Unknown event type: {EventType}",
                        fractalEvent.EventType);
                    break;
            }
        }
    }

    private Task HandleProcessData(FractalEvent @event, int workerId)
    {
        Logger.LogInformation(
            "Worker {WorkerId} processing data from {Source}: {Payload}",
            workerId, @event.SourceCellId, @event.Payload);

        return Task.Delay(100);
    }

    private Task HandleHeartbeat(FractalEvent @event, int workerId)
    {
        Logger.LogDebug(
            "Worker {WorkerId} heartbeat from {Source}",
            workerId, @event.SourceCellId);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Завершаем все TPL блоки в правильном порядке
        _inputBuffer.Complete();
        _mainProcessingBlock.Complete();
        _broadcastBlock.Complete();

        foreach (var workerBlock in _workerBlocks)
        {
            workerBlock.Complete();
        }

        // Ждем завершения всех блоков
        await Task.WhenAll(_workerBlocks.Select(b => b.Completion));
        await _broadcastBlock.Completion;
        await _mainProcessingBlock.Completion;
        await _inputBuffer.Completion;

        await base.StopAsync(cancellationToken);
    }
}

//public class TplFractalCell : FractalCellTemplate<TplInternalBus, TplExternalBus>
//{
//    private readonly List<BackgroundService> _services = new();
//    private readonly ActionBlock<IApplicationEvent> _mainProcessingBlock;
//    private readonly BroadcastBlock<IApplicationEvent> _broadcastBlock;
//    private readonly List<ActionBlock<IApplicationEvent>> _workerBlocks = new();

//    public TplFractalCell(
//        CellConfiguration configuration,
//        TplInternalBus internalBus,
//        TplExternalBus externalBus,
//        ILogger<TplFractalCell> logger)  // ← ДОБАВИТЬ ПАРАМЕТР
//        : base(configuration, internalBus, externalBus, logger)  // ← ПЕРЕДАТЬ В BASE
//    {
//        // Главный обрабатывающий блок
//        _mainProcessingBlock = new ActionBlock<IApplicationEvent>(
//            async @event => await ProcessInMainBlockAsync(@event),
//            new ExecutionDataflowBlockOptions
//            {
//                BoundedCapacity = configuration.ChannelCapacity,
//                MaxDegreeOfParallelism = configuration.MaxParallelism,
//                NameFormat = $"{configuration.CellId}-main"
//            });

//        // Broadcast для распределения событий между воркерами
//        _broadcastBlock = new BroadcastBlock<IApplicationEvent>(
//            e => e,
//            new DataflowBlockOptions
//            {
//                BoundedCapacity = configuration.ChannelCapacity,
//                NameFormat = $"{configuration.CellId}-broadcast"
//            });

//        // Связываем: Main → Broadcast (для дальнейшей маршрутизации)
//        _mainProcessingBlock.LinkTo(_broadcastBlock,                 
//            new DataflowLinkOptions { PropagateCompletion = true });
//    }

//    private async Task ProcessInMainBlockAsync(IApplicationEvent @event)
//    {
//        // Основная обработка
//        await InternalBus.PublishAsync(@event);

//        // Если нужно отправить во внешний мир
//        if (@event is FractalEvent fractalEvent &&
//            !string.IsNullOrEmpty(fractalEvent.TargetCellId))
//        {
//            await ExternalBus.SendToCellAsync(
//                fractalEvent.TargetCellId, fractalEvent);
//        }
//    }

//    protected override IEnumerable<BackgroundService> CreateBackgroundServices()
//    {
//        for (int i = 0; i < Configuration.BackgroundServiceCount; i++)
//        {
//            var workerBlock = new ActionBlock<IApplicationEvent>(
//                async @event => await ProcessInWorkerAsync(@event, i),
//                new ExecutionDataflowBlockOptions
//                {
//                    BoundedCapacity = Configuration.ChannelCapacity,
//                    MaxDegreeOfParallelism = 1, // Каждый воркер последователен
//                    NameFormat = $"{Configuration.CellId}-worker-{i}"
//                });

//            // Подключаем воркер к broadcast
//            _broadcastBlock.LinkTo(workerBlock,
//                new DataflowLinkOptions { PropagateCompletion = true });

//            _workerBlocks.Add(workerBlock);

//            yield return new TplBackgroundService(workerBlock,
//                $"{Configuration.CellId}-worker-{i}", Logger);
//        }
//    }

//    private async Task ProcessInWorkerAsync(IApplicationEvent @event, int workerId)
//    {
//        Logger.LogInformation("Worker {WorkerId} processing event {EventId}",
//            workerId, @event.EventId);

//        // Здесь логика конкретного воркера
//        if (@event is FractalEvent fractalEvent)
//        {
//            switch (fractalEvent.EventType)
//            {
//                case "ProcessData":
//                    await HandleProcessData(fractalEvent, workerId);
//                    break;
//                case "Heartbeat":
//                    await HandleHeartbeat(fractalEvent, workerId);
//                    break;
//                default:
//                    Logger.LogWarning("Unknown event type: {EventType}",
//                        fractalEvent.EventType);
//                    break;
//            }
//        }
//    }

//    private Task HandleProcessData(FractalEvent @event, int workerId)
//    {
//        Logger.LogInformation(
//            "Worker {WorkerId} processing data from {Source}: {Payload}",
//            workerId, @event.SourceCellId, @event.Payload);

//        // Имитация обработки
//        return Task.Delay(100);
//    }

//    private Task HandleHeartbeat(FractalEvent @event, int workerId)
//    {
//        Logger.LogDebug(
//            "Worker {WorkerId} heartbeat from {Source}",
//            workerId, @event.SourceCellId);

//        return Task.CompletedTask;
//    }

//    protected override void ConfigureHandlers()
//    {
//        // Подписка на события через InternalBus
//        InternalBus.Subscribe<FractalEvent>(async fractalEvent =>
//        {
//            // Отправляем в главный обрабатывающий блок
//            await _mainProcessingBlock.SendAsync(fractalEvent);
//        });
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        // Завершаем все TPL блоки
//        _mainProcessingBlock.Complete();
//        _broadcastBlock.Complete();

//        foreach (var workerBlock in _workerBlocks)
//        {
//            workerBlock.Complete();
//        }

//        // Ждем завершения всех блоков
//        await Task.WhenAll(_workerBlocks.Select(b => b.Completion));
//        await _broadcastBlock.Completion;
//        await _mainProcessingBlock.Completion;

//        await base.StopAsync(cancellationToken);
//    }
//}

// Вспомогательный класс для оборачивания TPL блока в BackgroundService
public class TplBackgroundService : BackgroundService
{
    private readonly ActionBlock<IApplicationEvent> _workerBlock;
    private readonly string _serviceId;
    private readonly ILogger _logger;

    public TplBackgroundService(
        ActionBlock<IApplicationEvent> workerBlock,
        string serviceId,
        ILogger logger)
    {
        _workerBlock = workerBlock;
        _serviceId = serviceId;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TplBackgroundService {ServiceId} started", _serviceId);

        try
        {
            await _workerBlock.Completion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TplBackgroundService {ServiceId}", _serviceId);
        }

        _logger.LogInformation("TplBackgroundService {ServiceId} stopped", _serviceId);
    }
}
