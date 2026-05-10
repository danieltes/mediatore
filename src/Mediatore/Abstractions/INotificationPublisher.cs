namespace Mediatore;

/// <summary>
/// Strategy for dispatching a notification to multiple handlers.
/// Implement this interface to customise dispatch behaviour (e.g., fire-and-forget).
/// </summary>
public interface INotificationPublisher
{
    Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken);
}
