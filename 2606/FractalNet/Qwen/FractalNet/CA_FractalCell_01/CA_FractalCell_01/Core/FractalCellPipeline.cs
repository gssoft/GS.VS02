using System.Threading.Tasks.Dataflow;

namespace FractalCell.Core;

/// <summary>
/// Ячейка = это Pipeline из TransformBlock'ов.
/// Каждое поведение — это блок в графе.
/// Событие течёт по графу естественно, без ручных WhenAll.
/// </summary>
public class FractalCellPipeline : IFractalCell
{
    public string CellId { get; }

    private readonly BufferBlock<IApplicationEvent> _input;
    private readonly List<BehaviorBlock> _behaviors = new();
    private readonly List<IDisposable> _links = new();
    private readonly BroadcastBlock<IApplicationEvent> _output;

    public FractalCellPipeline(string cellId)
    {
        CellId = cellId;

        _input = new BufferBlock<IApplicationEvent>(
            new DataflowBlockOptions { BoundedCapacity = 1000 });

        _output = new BroadcastBlock<IApplicationEvent>(e => e);

        // По умолчанию: вход -> выход (если нет поведений)
        _links.Add(_input.LinkTo(_output, new DataflowLinkOptions { PropagateCompletion = true }));
    }

    /// <summary>
    /// 🎯 ВОТ ОНА — ЛЁГКОСТЬ! Одна строка для добавления поведения.
    /// Приоритет = порядок подключения. Dataflow сам всё маршрутизирует.
    /// </summary>
    public FractalCellPipeline WithBehavior(BehaviorBlock behavior)
    {
        // Разрываем старую связь input->output
        foreach (var link in _links.Where(l => l is not null).ToList())
            link.Dispose();
        _links.Clear();

        var block = behavior.CreateBlock(this);
        _behaviors.Add(behavior);

        // Связываем: input -> behavior -> ... -> output
        _links.Add(_input.LinkTo(block, new DataflowLinkOptions { PropagateCompletion = true }));
        _links.Add(block.LinkTo(_output, new DataflowLinkOptions { PropagateCompletion = true }));

        return this; // Fluent API
    }

    public async Task SendAsync(IApplicationEvent @event, CancellationToken ct = default)
    {
        await _input.SendAsync(@event, ct);
    }

    /// <summary>
    /// Выходной BroadcastBlock — можно использовать для отправки ответов 
    /// или связи с другими ячейками.
    /// </summary>
    public ISourceBlock<IApplicationEvent> Output => _output;

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken ct)
    {
        _input.Complete();
        await _input.Completion;
        foreach (var link in _links) link.Dispose();
    }
}
