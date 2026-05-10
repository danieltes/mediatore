namespace Mediatore;

/// <summary>Marker for a streaming request that produces an async sequence of <typeparamref name="TResponse"/>.</summary>
public interface IStreamRequest<out TResponse> { }
