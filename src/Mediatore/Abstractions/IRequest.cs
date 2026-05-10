namespace Mediatore;

/// <summary>Marker for a request that produces a single <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse> { }
