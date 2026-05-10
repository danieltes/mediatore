namespace Mediatore;

/// <summary>
/// Encapsulates a single notification handler invocation for use by
/// <see cref="INotificationPublisher"/> implementations.
/// </summary>
public sealed class NotificationHandlerExecutor
{
    /// <summary>The concrete handler type, for diagnostics.</summary>
    public required Type HandlerType { get; init; }

    /// <summary>Invokes the handler with the given notification and cancellation token.</summary>
    public required Func<INotification, CancellationToken, Task> HandlerCallback { get; init; }
}
