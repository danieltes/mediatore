namespace Mediatore.Internal;

/// <summary>
/// Concrete wrapper that captures both <typeparamref name="TRequest"/> and
/// <typeparamref name="TResponse"/> at build time, allowing type-safe dispatch from a
/// type-erased call site.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : class, IRequest<TResponse>
{
    public Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var pipeline = PipelineBuilder.Build<TRequest, TResponse>((TRequest)request, sp, ct);
        return pipeline();
    }
}
