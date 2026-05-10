# Public API Contract: Mediatore Core Library

**Phase**: 1 — Design
**Date**: 2026-05-09
**Feature**: [spec.md](../spec.md) | [data-model.md](../data-model.md)

This document defines the public API surface that constitutes the stability contract for the
`Mediatore` NuGet package. All types and members listed here are covered by Semantic Versioning:
breaking changes require a MAJOR version bump (Constitution Art. VIII).

---

## Package: `Mediatore`

**Namespace root**: `Mediatore`

### Message Marker Interfaces

```csharp
namespace Mediatore;

/// <summary>Marker for a request that produces a single <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse> { }

/// <summary>Marker for a void command (returns <see cref="Unit"/>).</summary>
public interface ICommand : IRequest<Unit> { }

/// <summary>Marker for a fan-out notification with no return value.</summary>
public interface INotification { }

/// <summary>Marker for a streaming request that produces an async sequence of <typeparamref name="TResponse"/>.</summary>
public interface IStreamRequest<out TResponse> { }
```

---

### `Unit`

```csharp
namespace Mediatore;

/// <summary>
/// Represents the absence of a meaningful return value.
/// Used as the response type for <see cref="ICommand"/> implementations.
/// </summary>
public readonly record struct Unit
{
    /// <summary>The singleton value. Use instead of <c>new Unit()</c>.</summary>
    public static readonly Unit Value = new();
}
```

---

### Handler Interfaces

```csharp
namespace Mediatore;

/// <summary>Handles a <typeparamref name="TRequest"/> and returns a <typeparamref name="TResponse"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Handles a <typeparamref name="TCommand"/> that produces no meaningful return value.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Reacts to a <typeparamref name="TNotification"/> as part of a fan-out dispatch.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

/// <summary>Handles a streaming <typeparamref name="TRequest"/> and yields items lazily.</summary>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

---

### Pipeline Interfaces

```csharp
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
```

---

### Notification Publishing

```csharp
namespace Mediatore;

/// <summary>
/// Encapsulates a single notification handler invocation for use by
/// <see cref="INotificationPublisher"/> implementations.
/// </summary>
public sealed class NotificationHandlerExecutor
{
    /// <summary>The concrete handler type, for diagnostics.</summary>
    public Type HandlerType { get; init; }

    /// <summary>Invokes the handler with the given notification and cancellation token.</summary>
    public Func<INotification, CancellationToken, Task> HandlerCallback { get; init; }
}

/// <summary>
/// Strategy for dispatching a notification to multiple handlers.
/// Implement this interface to customise dispatch behaviour (e.g., fire-and-forget).
/// </summary>
public interface INotificationPublisher
{
    Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken);
}
```

---

### Built-in Publishers

```csharp
namespace Mediatore.Publishing;

/// <summary>
/// Awaits each notification handler sequentially in registration order.
/// Propagates the first exception immediately; subsequent handlers are not invoked.
/// </summary>
public sealed class SequentialPublisher : INotificationPublisher { ... }

/// <summary>
/// Runs all notification handlers concurrently via <c>Task.WhenAll</c>.
/// All exceptions are aggregated and thrown as <see cref="AggregateException"/>.
/// </summary>
public sealed class ParallelPublisher : INotificationPublisher { ... }
```

---

### The Mediator

```csharp
namespace Mediatore;

/// <summary>
/// Routes messages (requests, commands, notifications, streams) to their registered handlers.
/// Thread-safe; safe to register with <c>Singleton</c> lifetime.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request through the pipeline and returns the handler's response.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No handler is registered for <typeparamref name="TResponse"/>.</exception>
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a void command through the pipeline.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No handler is registered for <paramref name="command"/>.</exception>
    Task Send(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers using the configured
    /// <see cref="INotificationPublisher"/>.
    /// Zero handlers is not an error.
    /// </summary>
    Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Returns a lazily-evaluated async stream from the registered stream handler.
    /// </summary>
    /// <exception cref="HandlerNotFoundException">No stream handler registered for <typeparamref name="TResponse"/>.</exception>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
```

---

### Exceptions

```csharp
namespace Mediatore;

/// <summary>Base class for all Mediatore library exceptions.</summary>
public abstract class MediatorException : InvalidOperationException
{
    protected MediatorException(string message) : base(message) { }
    protected MediatorException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when <see cref="IMediator.Send"/> or <see cref="IMediator.CreateStream"/> is called
/// for a request type that has no registered handler.
/// </summary>
public sealed class HandlerNotFoundException : MediatorException
{
    /// <summary>The request type for which no handler was found.</summary>
    public Type RequestType { get; }

    public HandlerNotFoundException(Type requestType)
        : base($"No handler registered for request type '{requestType.FullName}'.") { ... }
}

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
               string.Join(", ", handlerTypes.Select(t => t.FullName))) { ... }
}
```

---

## Package: `Mediatore.Extensions.DependencyInjection`

**Namespace**: `Mediatore.Extensions.DependencyInjection`
**Dependency**: `Microsoft.Extensions.DependencyInjection.Abstractions`

```csharp
namespace Mediatore.Extensions.DependencyInjection;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all handlers found in the specified assemblies.
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<MediatorOptions>? configure = null);
}

public sealed class MediatorOptions
{
    /// <summary>
    /// Service lifetime applied to all registered handlers.
    /// Default: <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// The notification dispatch strategy.
    /// Default: <see cref="SequentialPublisher"/>.
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new SequentialPublisher();

    /// <summary>
    /// Registers all handlers from the assembly containing <typeparamref name="T"/>.
    /// </summary>
    public MediatorOptions RegisterServicesFromAssemblyContaining<T>();

    /// <summary>
    /// Registers all handlers from the specified assembly.
    /// </summary>
    public MediatorOptions RegisterServicesFromAssembly(Assembly assembly);
}
```

---

## Package: `Mediatore.SourceGenerator`

**Kind**: Roslyn `IIncrementalGenerator` — build-time only, no runtime assembly.
**Trigger**: Presence of `AddMediator(...)` call or `[MediatorHandler]` attribute on handler class.

### Emitted Extension Method

```csharp
// Generated file: MediatorRegistrations.g.cs
namespace Mediatore.Generated;

public static class GeneratedMediatorRegistrations
{
    /// <summary>
    /// Registers all handlers discovered at compile time.
    /// Call from AddMediator() instead of using assembly scanning.
    /// </summary>
    public static IServiceCollection RegisterGeneratedHandlers(this IServiceCollection services) { ... }
}
```

### Diagnostics

| Diagnostic ID | Severity | Condition | Message template |
|---|---|---|---|
| `MED0001` | `Error` | Duplicate `IRequestHandler<TRequest,TResponse>` for same `TRequest` | `Multiple handlers registered for '{RequestType}': {Handler1}, {Handler2}` |
| `MED0002` | `Warning` | Handler class is not `sealed` | `Handler '{HandlerType}' should be sealed (Constitution Art. IV.6)` |

---

## Stability Guarantees

| Scope | Guarantee |
|---|---|
| All interfaces in `Mediatore` namespace | Stable — breaking changes require MAJOR version bump |
| `Unit`, `NotificationHandlerExecutor`, exception types | Stable |
| `SequentialPublisher`, `ParallelPublisher` | Stable |
| `MediatorOptions` public properties | Stable |
| Types in `Mediatore.Internal.*` | **Unstable** — may change in any release |
| Generated code in `Mediatore.Generated.*` | **Unstable** — format may change; only the registration method signature is stable |
