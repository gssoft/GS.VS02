namespace FractalCell.Core.Common;

public class Unsubscriber : IDisposable
{
    private readonly Action _unsubscribeAction;
    private bool _disposed;

    public Unsubscriber(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _unsubscribeAction?.Invoke();
            _disposed = true;
        }
    }
}
