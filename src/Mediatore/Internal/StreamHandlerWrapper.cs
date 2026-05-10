namespace Mediatore.Internal;

/// <summary>
/// Concrete wrapper that captures both <typeparamref name="TRequest"/> and
/// <typeparamref name="TResponse"/> at build time for type-safe stream dispatch.
/// </summary>
internal sealed class StreamHandlerWrapper<TRequest, TResponse> : IStreamHandlerWrapper<TResponse>
    where TRequest : class, IStreamRequest<TResponse>
{
    public IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var handlerType = typeof(IStreamRequestHandler<TRequest, TResponse>);
        var handler = (IStreamRequestHandler<TRequest, TResponse>?)
            sp.GetService(handlerType)
            ?? throw new HandlerNotFoundException(typeof(TRequest));
        return handler.Handle((TRequest)request, ct);
    }
}
