namespace Mediatore.Publishing;

/// <summary>
/// Runs all notification handlers concurrently via <c>Task.WhenAll</c>.
/// All exceptions are aggregated and thrown as <see cref="AggregateException"/>.
/// </summary>
public sealed class ParallelPublisher : INotificationPublisher
{
    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var tasks = handlers
            .Select(executor =>
            {
                try { return executor.HandlerCallback(notification, cancellationToken); }
                catch (Exception ex) { return Task.FromException(ex); }
            })
            .ToList();

        if (tasks.Count == 0)
            return;

        var whenAll = Task.WhenAll(tasks);
        try
        {
            await whenAll.ConfigureAwait(false);
        }
        catch
        {
            // Re-throw AggregateException instead of the unwrapped first exception.
            if (whenAll.Exception is not null)
                throw whenAll.Exception;
            throw;
        }
    }
}
