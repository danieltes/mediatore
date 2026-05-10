namespace Mediatore;

/// <summary>Handles a streaming <typeparamref name="TRequest"/> and yields items lazily.</summary>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
