namespace Mediatore;

/// <summary>
/// Thrown when <see cref="IMediator.Send"/> or <see cref="IMediator.CreateStream"/> is called
/// for a request type that has no registered handler.
/// </summary>
public sealed class HandlerNotFoundException : MediatorException
{
    /// <summary>The request type for which no handler was found.</summary>
    public Type RequestType { get; }

    public HandlerNotFoundException(Type requestType)
        : base($"No handler registered for request type '{requestType.FullName}'.")
    {
        RequestType = requestType;
    }
}
