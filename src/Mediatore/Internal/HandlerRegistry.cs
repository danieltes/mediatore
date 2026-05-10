using System.Collections.Frozen;

namespace Mediatore.Internal;

/// <summary>
/// Immutable, thread-safe handler registry backed by <see cref="FrozenDictionary{TKey,TValue}"/>.
/// Populated at mediator build time (during <c>AddMediator</c>).
/// Raises <see cref="DuplicateHandlerException"/> when more than one handler is registered
/// for the same closed request type.
/// </summary>
internal sealed class HandlerRegistry
{
    private readonly FrozenDictionary<Type, object> _requestHandlers;
    private readonly FrozenDictionary<Type, IReadOnlyList<Type>> _notificationHandlers;
    private readonly FrozenDictionary<Type, object> _streamHandlers;

    /// <param name="requestHandlers">
    /// Sequence of (requestType, handlerType, wrapper) triples. Duplicate request types
    /// cause <see cref="DuplicateHandlerException"/> to be thrown.
    /// </param>
    /// <param name="notificationHandlers">
    /// Sequence of (notificationType, handlerType) pairs. Multiple handlers per notification
    /// type are allowed; they are preserved in registration order.
    /// </param>
    /// <param name="streamHandlers">
    /// Sequence of (streamRequestType, handlerType, wrapper) triples. Duplicate request types
    /// cause <see cref="DuplicateHandlerException"/> to be thrown.
    /// </param>
    internal HandlerRegistry(
        IEnumerable<(Type RequestType, Type HandlerType, object Wrapper)> requestHandlers,
        IEnumerable<(Type NotificationType, Type HandlerType)> notificationHandlers,
        IEnumerable<(Type RequestType, Type HandlerType, object Wrapper)> streamHandlers)
    {
        _requestHandlers = BuildRequestOrStreamDict(requestHandlers);
        _notificationHandlers = BuildNotificationDict(notificationHandlers);
        _streamHandlers = BuildRequestOrStreamDict(streamHandlers);
    }

    public bool TryGetRequestHandler(Type requestType, out object? wrapper)
        => _requestHandlers.TryGetValue(requestType, out wrapper);

    public bool TryGetStreamHandler(Type requestType, out object? wrapper)
        => _streamHandlers.TryGetValue(requestType, out wrapper);

    public IReadOnlyList<Type> GetNotificationHandlerTypes(Type notificationType)
        => _notificationHandlers.TryGetValue(notificationType, out var types)
            ? types
            : Array.Empty<Type>();

    private static FrozenDictionary<Type, object> BuildRequestOrStreamDict(
        IEnumerable<(Type RequestType, Type HandlerType, object Wrapper)> registrations)
    {
        var dict = new Dictionary<Type, (Type HandlerType, object Wrapper)>();
        Dictionary<Type, List<Type>>? duplicates = null;

        foreach (var (reqType, handlerType, wrapper) in registrations)
        {
            if (!dict.TryAdd(reqType, (handlerType, wrapper)))
            {
                duplicates ??= new Dictionary<Type, List<Type>>();
                if (!duplicates.TryGetValue(reqType, out var list))
                {
                    list = [dict[reqType].HandlerType];
                    duplicates[reqType] = list;
                }
                list.Add(handlerType);
            }
        }

        if (duplicates is { Count: > 0 })
        {
            var (reqType, handlerTypes) = duplicates.First();
            throw new DuplicateHandlerException(reqType, handlerTypes.AsReadOnly());
        }

        return dict.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.Wrapper);
    }

    private static FrozenDictionary<Type, IReadOnlyList<Type>> BuildNotificationDict(
        IEnumerable<(Type NotificationType, Type HandlerType)> registrations)
    {
        var dict = new Dictionary<Type, List<Type>>();

        foreach (var (notifType, handlerType) in registrations)
        {
            if (!dict.TryGetValue(notifType, out var list))
                dict[notifType] = list = [];
            list.Add(handlerType);
        }

        return dict.ToFrozenDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<Type>)kv.Value.AsReadOnly());
    }
}
