namespace FractalCellCore.Core.Templates;

public abstract class BackgroundBehaviorTemplate : BehaviorTemplate
{
	private Task? _backgroundTask;

	protected BackgroundBehaviorTemplate(ILogger? logger = null)
		: base(logger)
	{
	}

	protected override Task OnAttachedAsync(CancellationToken ct)
	{
		_backgroundTask = BackgroundLoopAsync(ct);
		return Task.CompletedTask;
	}

	protected override async Task OnDetachedAsync(CancellationToken ct)
	{
		if (_backgroundTask != null)
		{
			try { await _backgroundTask.WaitAsync(ct); }
			catch { }
		}
	}

	protected abstract Task BackgroundLoopAsync(CancellationToken ct);
}

