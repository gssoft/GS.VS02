// Services/NotificationHub.cs

namespace AspireApp1.Web.Services;

public class NotificationHub
{
    private readonly List<string> _notifications = new();
    private readonly object _lock = new();

    public void AddNotification(string message)
    {
        lock (_lock)
        {
            _notifications.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
            if (_notifications.Count > 20)
                _notifications.RemoveAt(_notifications.Count - 1);
        }
        OnNotificationReceived?.Invoke(message);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
        OnNotificationReceived?.Invoke(string.Empty);
    }

    public IReadOnlyList<string> GetNotifications()
    {
        lock (_lock) return _notifications.ToList();
    }

    public event Action<string>? OnNotificationReceived;
}
