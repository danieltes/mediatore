namespace Mediatore;

/// <summary>
/// A delegate representing the next stage in the pipeline (next behavior or the handler).
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Cross-cutting behavior that wraps handler execution for requests returning
/// <typeparamref name="TResponse"/>.
/// </summary>
/// <remarks>
/// Implementations MUST call <paramref name="next"/> exactly once on the happy path.
/// Intentional short-circuiting (not calling <paramref name="next"/>) must be documented
/// in the behavior's own specification.
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
