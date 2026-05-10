namespace Mediatore.Publishing;

/// <summary>
/// Awaits each notification handler sequentially in registration order.
/// Propagates the first exception immediately; subsequent handlers are not invoked.
/// </summary>
public sealed class SequentialPublisher : INotificationPublisher
{
    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var executor in handlers)
        {
            await executor.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
