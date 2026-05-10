namespace Mediatore.Internal;

/// <summary>
/// Builds the pipeline delegate chain that dispatches a request through zero or more
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> instances and finally to the handler.
/// </summary>
internal static class PipelineBuilder
{
    /// <summary>
    /// Builds a <see cref="RequestHandlerDelegate{TResponse}"/> that composes all registered
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> instances (in registration order)
    /// around the terminal handler resolved from <paramref name="serviceProvider"/>.
    /// </summary>
    internal static RequestHandlerDelegate<TResponse> Build<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TResponse>
    {
        // Terminal delegate — resolves and calls the handler
        RequestHandlerDelegate<TResponse> terminal = () =>
        {
            var handler =
                (IRequestHandler<TRequest, TResponse>?)
                serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>))
                ?? throw new HandlerNotFoundException(typeof(TRequest));

            return handler.Handle(request, cancellationToken);
        };

        // Resolve all registered behaviors for this request/response pair (in registration order)
        var behaviors = (IEnumerable<IPipelineBehavior<TRequest, TResponse>>?)
            serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>))
            ?? [];

        // Compose behaviors in reverse so the first-registered wraps the outermost layer
        return behaviors
            .Reverse()
            .Aggregate(terminal, (next, behavior) =>
                () => behavior.Handle(request, next, cancellationToken));
    }
}
