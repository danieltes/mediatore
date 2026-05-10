namespace Mediatore.Internal;

/// <summary>
/// Type-erased wrapper that dispatches an <see cref="IRequest{TResponse}"/> to its handler.
/// One instance is created per request type at mediator build time.
/// </summary>
internal interface IRequestHandlerWrapper<TResponse>
{
    Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
