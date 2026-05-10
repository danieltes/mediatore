namespace Mediatore;

/// <summary>
/// Thrown at mediator build time when two or more handlers are registered for the same request
/// type. Also emitted as a compiler Error diagnostic by the source generator.
/// </summary>
public sealed class DuplicateHandlerException : MediatorException
{
    /// <summary>The request type with conflicting registrations.</summary>
    public Type RequestType { get; }

    /// <summary>All handler types registered for <see cref="RequestType"/>.</summary>
    public IReadOnlyList<Type> HandlerTypes { get; }

    public DuplicateHandlerException(Type requestType, IReadOnlyList<Type> handlerTypes)
        : base($"Multiple handlers registered for '{requestType.FullName}': " +
               string.Join(", ", handlerTypes.Select(t => t.FullName)))
    {
        RequestType = requestType;
        HandlerTypes = handlerTypes;
    }
}
