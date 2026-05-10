namespace Mediatore;

/// <summary>Handles a <typeparamref name="TRequest"/> and returns a <typeparamref name="TResponse"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
