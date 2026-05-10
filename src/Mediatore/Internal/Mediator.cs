using Mediatore.Internal;

namespace Mediatore.Internal;

/// <summary>
/// Default implementation of <see cref="IMediator"/>.
/// Registered as <c>Singleton</c>; dispatches to handler instances resolved from
/// the <see cref="IServiceProvider"/> on every call.
/// </summary>
internal sealed class Mediator : IMediator
{
    private readonly HandlerRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationPublisher _publisher;

    public Mediator(
        HandlerRegistry registry,
        IServiceProvider serviceProvider,
        INotificationPublisher publisher)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _publisher = publisher;
    }

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        if (!_registry.TryGetRequestHandler(requestType, out var wrapper) || wrapper is null)
            throw new HandlerNotFoundException(requestType);

        var typedWrapper = (IRequestHandlerWrapper<TResponse>)wrapper;
        return typedWrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Send<Unit>(command, cancellationToken);
    }

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerTypes = _registry.GetNotificationHandlerTypes(notificationType);

        if (handlerTypes.Count == 0)
            return Task.CompletedTask;

        var executors = handlerTypes.Select(ht =>
        {
            var capturedHt = ht;
            return new NotificationHandlerExecutor
            {
                HandlerType = capturedHt,
                HandlerCallback = (n, ct) =>
                {
                    var handlerServiceType = typeof(INotificationHandler<TNotification>);
                    // For multiple handlers of the same notification type, resolve all
                    // and find the one matching capturedHt.
                    var handler = ResolveNotificationHandler<TNotification>(handlerServiceType, capturedHt);
                    if (handler is null)
                        throw new InvalidOperationException(
                            $"Notification handler '{capturedHt.FullName}' could not be resolved.");
                    return handler.Handle((TNotification)n, ct);
                }
            };
        });

        return _publisher.Publish(executors, notification, cancellationToken);
    }

    private INotificationHandler<TNotification>? ResolveNotificationHandler<TNotification>(
        Type handlerServiceType,
        Type handlerImplType)
        where TNotification : INotification
    {
        // Try to resolve the specific concrete type directly (works when only one registered).
        var direct = (INotificationHandler<TNotification>?)
            _serviceProvider.GetService(handlerServiceType);

        if (direct is not null && direct.GetType() == handlerImplType)
            return direct;

        // When multiple handlers exist, resolve by concrete type.
        return (INotificationHandler<TNotification>?)
            _serviceProvider.GetService(handlerImplType);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        if (!_registry.TryGetStreamHandler(requestType, out var wrapper) || wrapper is null)
            throw new HandlerNotFoundException(requestType);

        var typedWrapper = (IStreamHandlerWrapper<TResponse>)wrapper;
        return typedWrapper.Handle(request, _serviceProvider, cancellationToken);
    }
}
