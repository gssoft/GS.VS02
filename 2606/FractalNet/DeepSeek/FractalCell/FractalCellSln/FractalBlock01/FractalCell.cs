
using System.Threading.Tasks.Dataflow;
public class FractalCell : BackgroundService
{
    private readonly FractalBlock<IApplicationEvent> _block;

    protected FractalCell(string cellId, FractalBlock<IApplicationEvent>? parentBlock = null)
    {
        _block = new FractalBlock<IApplicationEvent>(cellId);
        parentBlock?.LinkTo(_block); // Подключаемся к родителю
    }

    protected void Subscribe<T>(Func<T, Task> handler) where T : IApplicationEvent
        => _block.Subscribe(handler);

    protected async Task SendToCell(FractalBlock<IApplicationEvent> targetBlock, IApplicationEvent @event)
        => await targetBlock.SendAsync(@event);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _block.Completion; // Ждем завершения блока
    }
}