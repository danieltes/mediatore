namespace Mediatore.Internal;

/// <summary>
/// Type-erased wrapper that dispatches an <see cref="IStreamRequest{TResponse}"/> to its handler.
/// One instance is created per stream request type at mediator build time.
/// </summary>
internal interface IStreamHandlerWrapper<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
