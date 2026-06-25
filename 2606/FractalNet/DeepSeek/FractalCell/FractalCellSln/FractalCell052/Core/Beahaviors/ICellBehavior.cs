using FractalCell02.Core.Interfaces;

namespace FractalCell02.Core.Behaviors;

/// <summary>
/// Контекст, который ячейка предоставляет своему поведению
/// </summary>
public interface ICellContext
{
    string CellId { get; }
    IExternalBus ExternalBus { get; }
    IInternalBus InternalBus { get; }
    CancellationToken StoppingToken { get; }
    ILogger Logger { get; }
}

/// <summary>
/// Реализация контекста ячейки
/// </summary>
public class CellContext : ICellContext
{
    public string CellId { get; }
    public IExternalBus ExternalBus { get; }
    public IInternalBus InternalBus { get; }
    public CancellationToken StoppingToken { get; set; }
    public ILogger Logger { get; }

    public CellContext(
        string cellId,
        IExternalBus externalBus,
        IInternalBus internalBus,
        ILogger logger,
        CancellationToken stoppingToken = default)
    {
        CellId = cellId;
        ExternalBus = externalBus;
        InternalBus = internalBus;
        Logger = logger;
        StoppingToken = stoppingToken;
    }
}

/// <summary>
/// Контракт поведения ячейки (бизнес-логика)
/// </summary>
public interface ICellBehavior
{
    /// <summary>
    /// Вызывается при старте ячейки
    /// </summary>
    Task OnStartAsync(ICellContext context);

    /// <summary>
    /// Вызывается при получении события из внутренней шины
    /// </summary>
    Task OnMessageAsync(IApplicationEvent @event, ICellContext context);

    /// <summary>
    /// Вызывается при остановке ячейки
    /// </summary>
    Task OnStopAsync(ICellContext context);
}
