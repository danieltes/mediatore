namespace Mediatore;

/// <summary>
/// Routes messages (requests, commands, notifications, streams) to their registered handlers.
/// Thread-safe; safe to register with <c>Singleton</c> lifetime.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request through the pipeline and returns the handler's response.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No handler is registered for <typeparamref name="TResponse"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a void command through the pipeline.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No handler is registered for <paramref name="command"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
    Task Send(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers using the configured
    /// <see cref="INotificationPublisher"/>.
    /// Zero handlers is not an error.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
    Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Returns a lazily-evaluated async stream from the registered stream handler.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No stream handler registered for <typeparamref name="TResponse"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
